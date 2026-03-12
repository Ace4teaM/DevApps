using DevApps.Record;
using IronPython.Runtime;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DevApps.Commands
{
    /// <summary>
    /// Serveur d'intéractions avec les modèles d'IA via le Model Context Protocol (MCP).
    /// </summary>
    internal static class CommandsServer
    {
        static CommandsServer()
        {
            // Méthodes statiques avec l'attribut RemoteCallAttribute
            RemoteMethods = Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic))
                .Where(m => m.GetCustomAttribute<RemoteCallAttribute>() != null)
                .ToArray();
        }

        internal static MethodInfo[] RemoteMethods { get; private set; }

        internal static CancellationTokenSource Cancel { get; private set; } = new CancellationTokenSource();

        /// <summary>
        /// Démarre le pipe de communication
        /// </summary>
        /// <remarks>Après la déconnexion, la boucle recommence et recrée un pipe</remarks>
        public static async Task Worker()
        {
            Program.Logger.WriteLine("Démarrage serveur de commandes en attente de connexion... " + Program.NamedPipePrefix + "commands");

            var assembly = Assembly.GetExecutingAssembly();

            while (Cancel.IsCancellationRequested == false)
            {
                var server = new NamedPipeServerStream(
                    Program.NamedPipePrefix + "commands",
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous
                );

                await server.WaitForConnectionAsync();

                Program.Logger.WriteLine("connexion... " + Program.NamedPipePrefix + "commands");

                // execute dans une tache parallèle pour pouvoir recréer un nouveau pipe immédiatement après la connexion d'un client (sinon le serveur ne peut accepter qu'une connexion à la fois et doit attendre la déconnexion du client pour recréer un nouveau pipe)
                _ = Task.Run(() => HandleClient(server));
            }
        }

        /// <summary>
        /// Execute le pipe de communication
        /// </summary>
        public static async Task HandleClient(NamedPipeServerStream server)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var output = new CommandResult();

            // client disposera writer et reader car il utilisent le même pipe (fermer writer ou reader ferme les 2)
            using var reader = new StreamReader(server, Encoding.UTF8, false, 1024, leaveOpen: true);

            try
            {
                var input = await reader.ReadLineAsync();

                if (input == null)
                    throw new Exception($"Aucune données fournit.");

                using JsonDocument doc = JsonDocument.Parse(input);
                JsonElement root = doc.RootElement;

                var methodName = root.GetProperty("method").GetString();

                if (methodName == null)
                    throw new Exception($"Le nom de la méthode n'est pas fournit.");

                var methodCall = RemoteMethods.FirstOrDefault(m => m.DeclaringType!.Name + "." + m.Name == methodName);

                if (methodCall == null)
                    throw new Exception($"Méthode '{methodName}' introuvable ou non autorisée.");

                var methodParams = methodCall.GetParameters();
                object[] invokedArgs = new object[methodParams.Length];

                // recherche la feature correspondante
                if (methodParams.Length > 0 && root.TryGetProperty("parameters", out var parametersNode))
                {
                    for (int i = 0; i < methodParams.Length; i++)
                    {
                        ParameterInfo p = methodParams[i];

                        // On cherche si la propriété existe dans le nœud 'parameters'
                        if (parametersNode.TryGetProperty(p.Name, out JsonElement val))
                        {
                            // On transforme le segment JSON vers le type C# attendu (int, string, class, etc.)
                            invokedArgs[i] = JsonSerializer.Deserialize(val.GetRawText(), p.ParameterType);
                        }
                        else if (p.HasDefaultValue)
                        {
                            invokedArgs[i] = p.DefaultValue;
                        }
                        else
                        {
                            throw new ArgumentException($"Le paramètre '{p.Name}' est requis par la méthode mais absent du JSON.");
                        }
                    }
                }

                // appelle la commande
                var title = methodCall.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "call " + methodName;

                await HistoryServices.BeginTransaction();

                try
                {
                    var result = methodCall.Invoke(null, invokedArgs);

                    if (result is Task task)
                    {
                        await task;
                        result = task.GetType().GetProperty("Result")?.GetValue(task);
                    }

                    output.Data = result;

                    HistoryServices.Commit(title);
                }
                catch (Exception ex)
                {
                    HistoryServices.Rollback();
                    Program.Logger.WriteLine(ex.Message);
                    throw; // renvoi le message d'erreur au client
                }

                output.Success = true;
            }
            catch (IOException ex)
            {
                output.Success = false;
                output.Message = ex.Message;
                Program.Logger.WriteLine("Erreur pipe : " + ex.Message);
            }
            catch (Exception ex)
            {
                output.Success = false;
                output.Message = ex.Message;
                Program.Logger.WriteLine("Command error : " + ex.Message);
            }
            finally
            {
                try
                {
                    if (server.IsConnected)
                    {
                        using var writer = new StreamWriter(server, Encoding.UTF8, 1024, leaveOpen: true) { AutoFlush = true };
                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(output);
                        await writer.WriteLineAsync(json);
                    }
                }
                catch (Exception ex2)
                {
                    Program.Logger.WriteLine("Response error : " + ex2.Message);
                }

                server.Dispose();
            }
        }

        internal static async Task Start()
        {
            await Worker();
        }

        internal static void Stop()
        {
            Cancel.Cancel();
        }
    }
}
