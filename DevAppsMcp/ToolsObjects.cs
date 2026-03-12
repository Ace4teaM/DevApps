using ModelContextProtocol.Server;
using System.ComponentModel;
using static DevAppsMcp.Program;

namespace DevAppsMcp
{
    [McpServerToolType]
    public class ToolsObjects
    {
        [McpServerTool, Description("Get object summary.")]
        public async Task<string> GetObjectSummary(
            [Description("identifiant du processus")] int processId,
            [Description("object name")] string name
            )
        {
            
            return await Program.RunCommand(processId, new { method = "Objects.Summary", parameters = new { name } });
        }

        [McpServerTool, Description("Get object data.")]
        public async Task<string> GetObjectData(
            [Description("identifiant du processus")] int processId,
            [Description("object name")] string name
            )
        {
            
            return await Program.RunCommand(processId, new { method = "Objects.GetData", parameters = new { name } });
        }

        [McpServerTool, Description("Get object names.")]
        public async Task<string> GetObjectsNames(
            [Description("identifiant du processus")] int processId
            )
        {
            
            return await Program.RunCommand(processId, new { method = "Objects.GetNames", parameters = new { } });
        }

        [McpServerTool, Description("Rename object.")]
        public async Task<string> RenameObject(
            [Description("identifiant du processus")] int processId,
            [Description("object name")] string name,
            [Description("New object name")] string newName
            )
        {
            
            return await Program.RunCommand(processId, new { method = "Objects.Rename", parameters = new { name, newName } });
        }

        [McpServerTool, Description("Create a new object.")]
        public async Task<string> CreateObject(
            [Description("identifiant du processus")] int processId,
            [Description("Base name for object to create")] string baseName,
            [Description("object description")] string description,
            [Description("object tags")] string[] tags
            )
        {
            
            return await Program.RunCommand(processId, new { method = "Objects.Create", parameters = new { baseName, description, tags } });
        }

        [McpServerTool, Description("Delete existing object.")]
        public async Task<string> DeleteObject(
            [Description("identifiant du processus")] int processId,
            [Description("Name of object to delete")] string name
            )
        {
            
            return await Program.RunCommand(processId, new { method = "Objects.Delete", parameters = new { name } });
        }

        [McpServerTool, Description("Delete multiples objects.")]
        public async Task<string> DeleteObjects(
            [Description("identifiant du processus")] int processId,
            [Description("Names of object to delete")] string[] names
            )
        {

            return await Program.RunCommand(processId, new { method = "Objects.Deletes", parameters = new { names } });
        }

        [McpServerTool, Description("Duplicate an existing object.")]
        public async Task<string> DuplicateObject(
            [Description("identifiant du processus")] int processId,
            [Description("objet name to duplicate")] string name
            )
        {
            
            return await Program.RunCommand(processId, new { method = "Objects.Duplicate", parameters = new { name } });
        }

        [McpServerTool, Description("Duplicate multiples objects.")]
        public async Task<string> DuplicateObjects(
            [Description("identifiant du processus")] int processId,
            [Description("Names of object to duplicate")] string[] names
            )
        {

            return await Program.RunCommand(processId, new { method = "Objects.Duplicates", parameters = new { names } });
        }
    }
}