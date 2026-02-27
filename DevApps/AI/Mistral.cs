using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static Program;

namespace DevApps.AI
{
    internal static class Mistral
    {
        internal class ChatMessage
        {
            public string? Role { get; set; }
            public string? Content { get; set; }
        }

        internal class Choice
        {
            public int? Index { get; set; }
            public ChatMessage? Message { get; set; }
            public string? Finish_reason { get; set; }
        }

        internal class Usage
        {
            public int? Prompt_tokens { get; set; }
            public int? Completion_tokens { get; set; }
            public int? Total_tokens { get; set; }
        }

        internal class ChatCompletionResponse
        {
            public string? Id { get; set; }
            public string? Object { get; set; }
            public long? Created { get; set; }
            public string? Model { get; set; }
            public List<Choice>? Choices { get; set; }
            public Usage? Usage { get; set; }
        }

        internal class ErrorResponse
        {
            public string? Message { get; set; }
            public string? Request_id { get; set; }
        }

        internal static bool TryParseError(string response, out ErrorResponse? errorResponse)
        {
            errorResponse = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(response);
            if (errorResponse == null)
                return false;

            if (errorResponse.Message == null)
                return false;

            return true;
        }

        internal static bool TryParseResponse(string response, out string? message)
        {
            message = null;
            
            var chatCompletionResponse = System.Text.Json.JsonSerializer.Deserialize<ChatCompletionResponse>(response);
            if (chatCompletionResponse == null || chatCompletionResponse.Choices == null || chatCompletionResponse.Choices.Count == 0)
                return false;

            message = chatCompletionResponse.Choices[0]?.Message?.Content;

            if (message == null)
                return false;

            if (message.Contains("```json"))
            {
                string start = "```json";
                string end = "```";

                int startIndex = message.IndexOf(start, StringComparison.InvariantCultureIgnoreCase);
                int endIndex = message.IndexOf(end, startIndex + start.Length, StringComparison.InvariantCultureIgnoreCase);

                if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
                {
                    message = message.Substring(startIndex + start.Length, endIndex - (startIndex + start.Length)).Trim();
                }
            }

            return true;
        }

        internal static async Task<string> SendFile(byte[] file, string filename)
        {
            Program.DevVariable? apiKey = null;
            Program.DevVariable? model = null;
            Program.DevVariable? endpoint = null;

            try
            {
                DevVariable._checkLock.Wait();

                Program.DevVariable.EnumPrivate().TryGetValue("MISTRAL_API_KEY", out apiKey);
                Program.DevVariable.EnumPrivate().TryGetValue("MISTRAL_MODEL", out model);
                Program.DevVariable.EnumPrivate().TryGetValue("MISTRAL_UPLOAD_URL", out endpoint);
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
                endpoint = new Program.DevVariable { Value = "https://api.mistral.ai/v1/files" };
            }

            if (model == null)
            {
                model = new Program.DevVariable { Value = "mistral-small" };
            }

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Value.ToString());

            var formData = new MultipartFormDataContent();

            // Ajoutez le fichier à envoyer
            var fileContent = new ByteArrayContent(file);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            formData.Add(fileContent, "file", Path.GetFileName(filename));

            var response = await client.PostAsync(endpoint.Value.ToString(), formData);
            return await response.Content.ReadAsStringAsync();
        }

        internal static async Task<string> Send(string message)
        {
            Program.DevVariable? apiKey = null;
            Program.DevVariable? model = null;
            Program.DevVariable? endpoint = null;

            try
            {
                DevVariable._checkLock.Wait();

                Program.DevVariable.EnumPrivate().TryGetValue("MISTRAL_API_KEY", out apiKey);
                Program.DevVariable.EnumPrivate().TryGetValue("MISTRAL_MODEL", out model);
                Program.DevVariable.EnumPrivate().TryGetValue("MISTRAL_URL", out endpoint);
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
                endpoint = new Program.DevVariable { Value = "https://api.mistral.ai/v1/chat/completions" };
            }

            if (model == null)
            {
                model = new Program.DevVariable { Value = "mistral-small" };
            }

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Value.ToString());
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var requestData = new
            {
                model = model.Value.ToString(),
                messages = new[]
                {
                    new { role = "system", content = Profile.GetContext() },
                    new { role = "user", content = message }
                },
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(endpoint.Value.ToString(), content);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
