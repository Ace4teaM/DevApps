using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using static DevAppsMcp.Program;

namespace DevAppsMcp
{
    [McpServerToolType]
    public class ToolsProject
    {
        private readonly SessionStateManager _stateManager;

        public ToolsProject(SessionStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        [McpServerTool, Description("Enum available processes.")]
        public async Task<string> EnumProcess()
        {
            var processList = new List<dynamic>();
            foreach (var proc in System.Diagnostics.Process.GetProcesses().Where(p => p.ProcessName == "DevApps"))
            {
                try
                {
                    var infos = await UserContext.GetProcessInfos(proc.Id);
                    processList.Add(new
                    {
                        Id = proc.Id,
                        Infos = infos
                    });
                }
                catch
                {
                    continue;
                }
            }

            var result = new CommandResult
            {
                Success = true,
                Message = "Process ids",
                Data = new { process = processList }
            };

            return JsonSerializer.Serialize(result);
        }

        [McpServerTool, Description("Obtains the markdown development log explaining the main points of the project.")]
        public async Task<string> GetProjectDevLog(
            [Description("identifiant du processus")] int processId
            )
        {
            
            return await UserContext.RunCommand(processId, new { method = "Project.ReadDevLog", parameters = new {  } });
        }

        [McpServerTool, Description("Get project summary.")]
        public async Task<string> GetProjectSummary(
            [Description("identifiant du processus")] int processId
            )
        {
            
            return await UserContext.RunCommand(processId, new { method = "Project.Summary", parameters = new { } });
        }

        [McpServerTool, Description("Get project data.")]
        public async Task<string> GetProjectData(
            [Description("identifiant du processus")] int processId
            )
        {
            
            return await UserContext.RunCommand(processId, new { method = "Project.GetData", parameters = new { } });
        }

    }
}