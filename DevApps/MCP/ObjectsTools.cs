using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http;

namespace DevApps.MCP
{
    [McpServerToolType]
    public static class ObjectsTools
    {
        [McpServerTool, Description("Create a new object.")]
        public static async Task<string> CreateObject(HttpClient client)
        {
            DevApps.Features.Objects.Create(out var name);
            return name;
        }

        [McpServerTool, Description("Duplicate an existing object.")]
        public static async Task<string> DuplicateObject(
            HttpClient client,
            [Description("Name of objet to duplicate")] string name)
        {
            return DevApps.Features.Objects.Duplicate(name);
        }
    }
}