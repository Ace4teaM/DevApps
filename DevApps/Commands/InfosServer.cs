using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;

namespace DevApps.Commands
{
    /// <summary>
    /// Serveur d'intéractions avec les modèles d'IA via le Model Context Protocol (MCP).
    /// </summary>
    internal static class InfosServer
    {
        internal static readonly string VersionTag = "VERSION:";
        internal static readonly string ProjectTag = "PROJECT:";
        internal static readonly string CommandsTag = "COMMANDS:";

        public static Task? WorkerTask = null;

        /// <summary>
        /// Démarre le pipe de communication pour les informations
        /// </summary>
        /// <remarks>Après la déconnexion, la boucle recommence et recrée un pipe</remarks>
        public static async Task Worker(CancellationToken token)
        {
            Program.Logger.WriteLine("Démarrage serveur d'informations en attente de connexion... " + Program.NamedPipePrefix + "infos");
            while (token.IsCancellationRequested == false)
            {
                using var server = new NamedPipeServerStream(
                    Program.NamedPipePrefix + "infos",
                    PipeDirection.Out,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous
                );

                await server.WaitForConnectionAsync(token);

                Program.Logger.WriteLine("connexion... " + Program.NamedPipePrefix + "infos");

                // execute dans une tache parallèle pour pouvoir recréer un nouveau pipe immédiatement après la connexion d'un client (sinon le serveur ne peut accepter qu'une connexion à la fois et doit attendre la déconnexion du client pour recréer un nouveau pipe)
                _ = Task.Run(() => HandleClient(server));
            }
        }

        /// <summary>
        /// Execute le pipe de communication
        /// </summary>
        public static async Task HandleClient(NamedPipeServerStream server)
        {
            try
            {
                Program.Logger.WriteLine("connexion... " + Program.NamedPipePrefix + "infos");

                using var writer = new StreamWriter(server, Encoding.UTF8) { AutoFlush = true };

                var assembly = Assembly.GetExecutingAssembly();

                var project = Path.GetFileName(Environment.CurrentDirectory);

                await writer.WriteLineAsync(VersionTag + assembly.GetName().Version?.ToString());
                await writer.WriteLineAsync(ProjectTag + project);
                await writer.WriteLineAsync(CommandsTag + string.Join(",", CommandsServer.RemoteMethods.Select(p => p.Name)));
            }
            catch (IOException ex)
            {
                Program.Logger.WriteLine("Erreur pipe : " + ex.Message);
            }
        }

        internal static void Start(CancellationToken token)
        {
            WorkerTask = Worker(token);
        }

        internal static void Wait()
        {
            if (WorkerTask != null && WorkerTask.Status == TaskStatus.Running)
            {
                WorkerTask.Wait();
            }
        }
    }
}
