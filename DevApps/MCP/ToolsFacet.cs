using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DevApps.MCP
{
    [McpServerToolType]
    public static class ToolsFacets
    {
        [McpServerTool, Description("move objet in view.")]
        public static async Task<string> MoveObject(
            [Description("facet name")] string name,
            [Description("object name")] string objectName,
            [Description("left position")] double x,
            [Description("top position")] double y,
            [Description("width")] double w,
            [Description("height")] double h
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Facets.MoveObject(name, objectName, x, y, w, h),
                () => new { }
            );
        }

        [McpServerTool, Description("remove object in view.")]
        public static async Task<string> RemoveObject(
            [Description("facet name")] string name,
            [Description("object name")] string objectName
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Facets.RemoveObject(name, objectName),
                () => new { }
            );
        }

        [McpServerTool, Description("add object to view.")]
        public static async Task<string> AddObject(
            [Description("facet name")] string name,
            [Description("object name")] string objectName,
            [Description("left position")] double x,
            [Description("top position")] double y,
            [Description("width")] double w,
            [Description("height")] double h
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Facets.AddObject(name, objectName, x, y, w, h),
                () => new { }
            );
        }

        [McpServerTool, Description("remove geometry in view.")]
        public static async Task<string> RemoveGeomerty(
            [Description("facet name")] string name,
            [Description("geometry guid")] string guid
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Facets.RemoveGeomerty(name, guid),
                () => new { }
            );
        }

        [McpServerTool, Description("add geometry in view.")]
        public static async Task<string> AddGeomerty(
            [Description("facet name")] string name,
            [Description("geometry path")] string path,
            [Description("left position")] double x,
            [Description("top position")] double y
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Facets.AddGeomerty(name, path, x, y),
                () => new { }
            );
        }

        [McpServerTool, Description("build facet.")]
        public static async Task<string> Build(
            [Description("facet name")] string name
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Facets.Build(name),
                () => new { }
            );
        }

        [McpServerTool, Description("Get facet names.")]
        public static async Task<string> ListFacets()
        {
            return await McpResult.MakeJson(
                DevApps.Features.Facets.GetNames(),
                (ret) => new { FacetsNames = ret }
            );
        }

        [McpServerTool, Description("Get facet summary.")]
        public static async Task<string> FacetSummary(
            [Description("facet name")] string name
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Facets.Summary(name),
                (ret) => new { Summary = ret, FacetName = name }
            );
        }

        [McpServerTool, Description("Get facet data.")]
        public static async Task<string> GetFacetData(
            [Description("facet name")] string name
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Facets.GetData(name),
                (ret) => new { Data = ret, FacetName = name }
            );
        }

        [McpServerTool, Description("Create a new facet.")]
        public static async Task<string> CreateFacet(
            [Description("Base name for facet to create")] string name,
            [Description("Nom des objets")] string[] objectNames
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Facets.Create(name, objectNames),
                (ret) => new { CreatedFacetName = ret }
            );
        }

        [McpServerTool, Description("Delete existing facet.")]
        public static async Task<string> DeleteFacet(
            [Description("Name of facet to delete")] string name
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Facets.Delete(name),
                () => new { }
            );
        }
    }
}
