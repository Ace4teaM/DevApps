using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Windows.Forms;

namespace DevApps.MCP
{
    [McpServerToolType]
    public static class ToolsObjects
    {

        [McpServerTool, Description("Get object summary.")]
        public static async Task<string> ObjectSummary(
            [Description("object name")] string name
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Objects.Summary(name),
                (ret) => new { Summary = ret, ObjectName = name }
            );
        }

        [McpServerTool, Description("Get object data.")]
        public static async Task<string> GetObjectData(
            [Description("object name")] string name
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Objects.GetData(name),
                (ret) => new { Data = ret, ObjectName = name }
            );
        }

        [McpServerTool, Description("Get object names.")]
        public static async Task<string> ListObjects()
        {
            return await McpResult.MakeJson(
                DevApps.Features.Objects.GetNames(),
                (ret) => new { ObjectsNames = ret }
            );
        }

        [McpServerTool, Description("Rename object.")]
        public static async Task<string> Rename(
            [Description("object name")] string name,
            [Description("New object name")] string newName
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Objects.Rename(name, newName),
                () => new { }
            );
        }

        [McpServerTool, Description("Create a new object.")]
        public static async Task<string> CreateObject(
            [Description("Base name for object to create")] string name,
            [Description("object description")] string description,
            [Description("object tags")] string[] tags
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Objects.Create(name, description, tags),
                (ret) => new { CreatedObjectName = ret }
            );
        }

        [McpServerTool, Description("Delete existing object.")]
        public static async Task<string> DeleteObject(
            [Description("Name of object to delete")] string name
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Objects.Delete(name),
                () => new { }
            );
        }

        [McpServerTool, Description("Duplicate an existing object.")]
        public static async Task<string> DuplicateObject(
            [Description("objet name to duplicate")] string name
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Objects.Duplicate(name),
                (ret) => new { Name = ret }
            );
        }
    }
}