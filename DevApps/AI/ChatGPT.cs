using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static Program;

namespace DevApps.AI
{
    internal static class ChatGPT
    {
        internal class ApiError
        {
            public string? Message {get;set;}
            public string? Type {get;set;}
            public string? Param {get;set;}
            public string? Code {get;set;}
        }
        internal class ErrorResponse
        {
            public ApiError? Error { get; set; }
        }

        internal static bool TryParseError(string response, out ErrorResponse? errorResponse)
        {
            errorResponse = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(response);
            if (errorResponse == null || errorResponse.Error == null)
                return false;

            if (errorResponse.Error.Message == null)
                return false;

            return true;
        }
        internal static async Task<string> Send(string message)
        {
            Program.DevVariable? apiKey = null;
            Program.DevVariable? model = null;
            Program.DevVariable? endpoint = null;

            try
            {
                DevVariable._checkLock.Wait();

                Program.DevVariable.EnumPrivate().TryGetValue("CHATGPT_API_KEY", out apiKey);
                Program.DevVariable.EnumPrivate().TryGetValue("CHATGPT_MODEL", out model);//"gpt-4" ou "gpt-3.5-turbo"
                Program.DevVariable.EnumPrivate().TryGetValue("CHATGPT_URL", out endpoint);//https://api.openai.com/v1/chat/completions
            }
            finally
            {
                DevVariable._checkLock.Release();
            }

            if (apiKey == null)
            {
                throw new Exception("API key not found.");
            }

            if (endpoint == null)
            {
                endpoint = new Program.DevVariable("endpoint", "https://api.openai.com/v1/chat/completions");
            }

            if (model == null)
            {
                model = new Program.DevVariable("model", "gpt-3.5-turbo");
            }

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Value.ToString());
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var requestBody = new
            {
                model = model.Value.ToString(),
                messages = new[]
                {
                    new { role = "system", content = Profile.GetContext() },
                    new { role = "user", content = message }
                },
                temperature = 0.7
            };

            string json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(endpoint.Value.ToString(), content);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
