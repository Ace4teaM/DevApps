using ModelContextProtocol.Server;
using System.ComponentModel;
using static DevAppsMcp.Program;

namespace DevAppsMcp
{
    [McpServerToolType]
    public class ToolsFacets
    {
        private readonly SessionStateManager _stateManager;

        public ToolsFacets(SessionStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        [McpServerTool, Description("move objet in view.")]
        public async Task<string> MoveObjectInFacet(
            [Description("identifiant du processus")] int processId,
            [Description("facet name")] string name,
            [Description("object name")] string objectName,
            [Description("left position")] double x,
            [Description("top position")] double y,
            [Description("width")] double w,
            [Description("height")] double h
            )
        {
            
            return await UserContext.RunCommand(processId, new{ method = "Facets.MoveObject", parameters = new{ name, objectName, x, y, w, h} });
        }

        [McpServerTool, Description("remove object in view.")]
        public async Task<string> RemoveObjectInFacet(
            [Description("identifiant du processus")] int processId,
            [Description("facet name")] string name,
            [Description("object name")] string objectName
            )
        {
            
            return await UserContext.RunCommand(processId, new { method = "Facets.RemoveObject", parameters = new { name, objectName } });
        }

        [McpServerTool, Description("add object to view.")]
        public async Task<string> AddObjectInFacet(
            [Description("identifiant du processus")] int processId,
            [Description("facet name")] string name,
            [Description("object name")] string objectName,
            [Description("left position")] double x,
            [Description("top position")] double y,
            [Description("width")] double w,
            [Description("height")] double h
            )
        {
            
            return await UserContext.RunCommand(processId, new { method = "Facets.AddObject", parameters = new { name, objectName, x, y, w, h } });
        }

        [McpServerTool, Description("remove geometry in view.")]
        public async Task<string> RemoveGeometryInFacet(
            [Description("identifiant du processus")] int processId,
            [Description("facet name")] string name,
            [Description("geometry guid")] string guid
            )
        {
            
            return await UserContext.RunCommand(processId, new { method = "Facets.RemoveGeometry", parameters = new { name, guid } });
        }

        [McpServerTool, Description("add geometry in view.")]
        public async Task<string> AddGeometryInFacet(
            [Description("identifiant du processus")] int processId,
            [Description("facet name")] string name,
            [Description("geometry path")] string path,
            [Description("left position")] double x,
            [Description("top position")] double y
            )
        {
            
            return await UserContext.RunCommand(processId, new { method = "Facets.AddGeometry", parameters = new { name, path, x, y } });
        }

        [McpServerTool, Description("build facet.")]
        public async Task<string> BuildFacet(
            [Description("identifiant du processus")] int processId,
            [Description("facet name")] string name
            )
        {
            
            return await UserContext.RunCommand(processId, new { method = "Facets.Build", parameters = new { name } });
        }

        [McpServerTool, Description("Get facet names.")]
        public async Task<string> GetFacetNames(
            [Description("identifiant du processus")] int processId
         )
        {
            
            return await UserContext.RunCommand(processId, new { method = "Facets.GetNames", parameters = new { } });
        }

        [McpServerTool, Description("Get facet summary.")]
        public async Task<string> GetFacetSummary(
            [Description("identifiant du processus")] int processId,
            [Description("facet name")] string name
            )
        {
            
            return await UserContext.RunCommand(processId, new { method = "Facets.Summary", parameters = new { name } });
        }

        [McpServerTool, Description("Get facet data.")]
        public async Task<string> GetFacetData(
            [Description("identifiant du processus")] int processId,
            [Description("facet name")] string name
            )
        {
            
            return await UserContext.RunCommand(processId, new { method = "Facets.GetData", parameters = new { name } });
        }

        [McpServerTool, Description("Create a new facet.")]
        public async Task<string> CreateFacet(
            [Description("identifiant du processus")] int processId,
            [Description("Base name for facet to create")] string baseName,
            [Description("Nom des objets")] string[] objectNames
            )
        {
            
            return await UserContext.RunCommand(processId, new { method = "Facets.Create", parameters = new { baseName, objectNames } });
        }

        [McpServerTool, Description("Delete existing facet.")]
        public async Task<string> DeleteFacet(
            [Description("identifiant du processus")] int processId,
            [Description("Name of facet to delete")] string name
            )
        {
            
            return await UserContext.RunCommand(processId, new { method = "Facets.Delete", parameters = new { name } });
        }
    }
}
