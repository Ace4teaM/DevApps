using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DevApps.MCP
{
    [McpServerToolType]
    public static class ToolsProject
    {
        [McpServerTool, Description("Obtains the markdown development log explaining the main points of the project.")]
        public static async Task<string> ReadDevLog()
        {
            return await McpResult.MakeJson(
                DevApps.Features.Project.ReadDevLog(),
                (ret) => new { Content = ret }
            );
        }

        [McpServerTool, Description("Get project summary.")]
        public static async Task<string> ProjectSummary()
        {
            return await McpResult.MakeJson(
                DevApps.Features.Project.Summary(),
                (ret) => new { Summary = ret }
            );
        }

        [McpServerTool, Description("Get project data.")]
        public static async Task<string> GetProjectData()
        {
            return await McpResult.MakeJson(
                DevApps.Features.Project.GetData(),
                (ret) => new { Data = ret }
            );
        }

    }
}