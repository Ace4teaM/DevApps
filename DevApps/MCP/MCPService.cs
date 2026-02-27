using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;
using System.Net.Http;
using System.Net.Http.Headers;

namespace DevApps.MCP
{
    /// <summary>
    /// Serveur d'intéractions avec les modèles d'IA via le Model Context Protocol (MCP).
    /// </summary>
    internal static class MCPService
    {
        internal static Task? Current { get; private set; }
        internal static IHost? CurrentHost { get; private set; }
        internal static CancellationTokenSource Cancel { get; private set; } = new CancellationTokenSource();

        internal static void Start()
        {
            var builder = Host.CreateEmptyApplicationBuilder(settings: null);

            builder.Services.AddMcpServer()
                .WithStdioServerTransport() // utilise les flux d'entrée/sortie standard pour la communication
                //.WithStreamServerTransport() // utilise un flux personnalisé (ex: TCP) pour la communication
                .WithToolsFromAssembly(); // scan l'assembly pour trouver les outils à exposer [McpServerTool]

            CurrentHost = builder.Build();

            Current = CurrentHost.RunAsync(Cancel.Token);
        }

        internal static void Stop()
        {
            Cancel.Cancel();

            // attendre la fin du service
            CurrentHost?.StopAsync().Wait();
        }
    }
}
