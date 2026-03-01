using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;


namespace DevApps.MCP
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

}
