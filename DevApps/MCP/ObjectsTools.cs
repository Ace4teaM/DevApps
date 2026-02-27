using Microsoft.CodeAnalysis.CSharp.Syntax;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using static DevApps.MCP.ObjectsTools;
using static IronPython.Modules._ast;

namespace DevApps.MCP
{
    [McpServerToolType]
    public static class ObjectsTools
    {
        /// <summary>
        /// Type uniforme de retour des outils MCP, avec sérialisation JSON intégrée et gestion des exceptions.
        /// </summary>
        public class McpResult
        {
            public static async Task<string> MakeJson(Task task, Func<object?> val)
            {
                var result = new McpResult();
                try
                {
                    await task;
                    result.Success = task.IsCompletedSuccessfully;
                    result.Data = val();
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new McpResult
                    {
                        Success = false,
                        Message = ex.Message
                    });
                }
                return JsonSerializer.Serialize(result);
            }
            public static async Task<string> MakeJson<T>(Task<T> task, Func<T, object?> val)
            {
                var result = new McpResult();
                try
                {
                    var data = await task;
                    result.Success = task.IsCompletedSuccessfully;
                    result.Data = val(data);
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new McpResult
                    {
                        Success = false,
                        Message = ex.Message
                    });
                }
                return JsonSerializer.Serialize(result);
            }
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public object? Data { get; set; } // json
        }

        [McpServerTool, Description("Get object names.")]
        public static async Task<string> ListObjects()
        {
            return await McpResult.MakeJson(
                DevApps.Features.Objects.GetNames(),
                (ret) => new { ObjectsNames = ret }
            );
        }

        [McpServerTool, Description("Create a new object.")]
        public static async Task<string> CreateObject(
            [Description("Base name for objet to create")] string name
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Objects.Create(name),
                (ret) => new { CreatedObjectName = ret }
            );
        }

        [McpServerTool, Description("Delete existing object.")]
        public static async Task<string> DeleteObject(
            [Description("Name of objet to delete")] string name
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Objects.Delete(name),
                () => new { }
            );
        }

        [McpServerTool, Description("Duplicate an existing object.")]
        public static async Task<string> DuplicateObject(
            [Description("Name of objet to duplicate")] string name
            )
        {
            return await McpResult.MakeJson(
                DevApps.Features.Objects.Duplicate(name),
                (ret) => new { Name = ret }
            );
        }
    }
}