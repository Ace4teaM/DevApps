using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Threading;

namespace DevAppsMcp
{
    public class Program
    {
        /// <summary>
        /// Temps avant abandon d'une commande (en ms)
        /// </summary>
        public static readonly int CommandTimeoutMs = 60000;

        public static async Task<Dictionary<string, string>> GetProcessInfos(int ProcessId)
        {
            using var client = new NamedPipeClientStream(".", String.Format("devapps-{0}.infos", ProcessId), PipeDirection.In, PipeOptions.Asynchronous);

            using var cts = new CancellationTokenSource(CommandTimeoutMs);
            await client.ConnectAsync(cts.Token);

            using var reader = new StreamReader(client, Encoding.UTF8);

            Dictionary<string, string> infos = new();

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var split = line.Split(':', 2);
                infos.Add(split[0].Trim(), split[1].Trim());
            }

            return infos;
        }

        public static async Task<string> RunCommand(int ProcessId, dynamic obj)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", String.Format("devapps-{0}.commands", ProcessId), PipeDirection.InOut, PipeOptions.Asynchronous);

                using var cts = new CancellationTokenSource(CommandTimeoutMs);
                await client.ConnectAsync(cts.Token);

                string json = JsonConvert.SerializeObject(obj);

                // client disposera writer et reader car il utilisent le même pipe (fermer writer ou reader ferme les 2)
                using var writer = new StreamWriter(client, Encoding.UTF8, 1024, leaveOpen: true) { AutoFlush = true };
                using var reader = new StreamReader(client, Encoding.UTF8, false, 1024, leaveOpen: true);

                await writer.WriteLineAsync(json);

                var input = await reader.ReadLineAsync();

                if (input == null)
                    throw new Exception($"Aucune données fournit.");

                return input;
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new CommandResult { Data = null, Message = "Une erreur de communication est survenue." + ex.Message, Success = false });
            }
        }

        internal static string Summary
        {
            get
            {
                // Nom complet de la ressource : <Namespace>.<Dossier>.<Fichier>
                string resourceName = "DevAppsMcp.Resources.Summary.md";

                var assembly = Assembly.GetExecutingAssembly();

                using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    throw new Exception($"Ressource intégrée '{resourceName}' non trouvée dans l'assembly.");
                }

                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
        }

        public static void Main(string[] args)
        {
           var assembly = Assembly.GetExecutingAssembly();

            var builder = Host.CreateApplicationBuilder(args);

            builder.Logging.ClearProviders();

            builder.Services.AddMcpServer()
                .WithResources(new Dictionary<string, object>
                {
                    ["ServerInfo"] = new
                    {
                        Name = assembly.GetCustomAttributes<AssemblyTitleAttribute>().FirstOrDefault()?.Title ?? "N/A",
                        Version = assembly.GetName().Version?.ToString() ?? "N/A",
                        Description = "DevApps MCP Server",
                        Author = "Thomas AUGUEY",
                        Summary = Summary
                    }/* ,
                    ["Config"] = new
                    {
                        MaxConnections = 10,
                        EnableLogging = true
                    }*/
                })
                .WithStdioServerTransport()
                .WithTools<ToolsObjects>()
                .WithTools<ToolsFacets>()
                .WithTools<ToolsProject>();

            var host = builder.Build();
            host.Run();
        }
    }
}