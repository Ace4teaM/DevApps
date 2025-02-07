using IronPython.Runtime;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace DevApps.PythonExtends
{
    public class Requests
    {
        public HttpResponseMessage? post(string url, PythonDictionary headers, PythonDictionary json)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Créer un StringContent avec le JSON et spécifier le type de contenu (application/json)
                    var dict = new Dictionary<string, object>();
                    foreach (var key in json.Keys)
                    {
                        dict.Add(key.ToString(), json[key]);
                    }
                    string jsonContent = JsonConvert.SerializeObject(dict, Formatting.Indented);

                    StringContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // Ajouter des headers à la requête
                    foreach (var item in headers)
                    {
                        var key = item.Key.ToString();
                        var val = item.Value.ToString();
                        client.DefaultRequestHeaders.Add(key, val);
                    }

                    // Envoyer la requête POST
                    HttpResponseMessage response = client.PostAsync(url, content).Result;

                    // Vérifier si la réponse est réussie
                    if (response.IsSuccessStatusCode)
                    {
                        // Lire le contenu de la réponse
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        System.Console.WriteLine("Réponse reçue : ");
                        System.Console.WriteLine(responseContent);
                    }
                    else
                    {
                        System.Console.WriteLine($"Erreur : {response.StatusCode}");
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Une erreur s'est produite : {ex.Message}");
                }
            }

            return null;
        }
        public string json()
        {
            return string.Empty;
        }
    }
}
