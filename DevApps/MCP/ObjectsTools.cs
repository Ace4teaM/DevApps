using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Net.Http;

namespace DevApps.MCP
{
    [McpServerToolType]
    public static class ObjectsTools
    {
        [McpServerTool, Description("Create a new object.")]
        public static async Task<string> CreateObject()
        {
            return await DevApps.Features.Objects.Create("NewObject");
        }

        [McpServerTool, Description("Duplicate an existing object.")]
        public static async Task<string?> DuplicateObject(
            [Description("Name of objet to duplicate")] string name
            )
        {
            return await DevApps.Features.Objects.Duplicate(name);
        }
    }
}