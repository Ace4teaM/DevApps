using Markdig.Wpf;
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

        internal static CancellationTokenSource Cancel { get; private set; } = new CancellationTokenSource();

        /// <summary>
        /// Démarre le pipe de communication pour les informations
        /// </summary>
        /// <remarks>Après la déconnexion, la boucle recommence et recrée un pipe</remarks>
        public static async Task Worker()
        {
            while (Cancel.IsCancellationRequested == false)
            {
                using var server = new NamedPipeServerStream(
                    Program.NamedPipePrefix + "infos",
                    PipeDirection.Out,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous
                );

                Console.WriteLine("Serveur d'informations en attente de connexion... "+ Program.NamedPipePrefix + "infos");
                try
                {
                    await server.WaitForConnectionAsync(Cancel.Token);
                    Console.WriteLine("Client connecté !");

                    using var writer = new StreamWriter(server, Encoding.UTF8) { AutoFlush = true };

                    var assembly = Assembly.GetExecutingAssembly();

                    var project = Path.GetFileName(Environment.CurrentDirectory);

                    await writer.WriteLineAsync(VersionTag + assembly.GetName().Version?.ToString());
                    await writer.WriteLineAsync(ProjectTag + project);
                    await writer.WriteLineAsync(CommandsTag + string.Join(",", CommandsServer.RemoteMethods.Select(p=>p.Name)));
                }
                catch (IOException ex)
                {
                    Program.Logger.WriteLine("Erreur pipe : " + ex.Message);
                }
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
