using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace DevApps.AI
{
    /// <summary>
    /// Implément les fonctions pour initialiser le 'profile' de l'agent IA
    /// </summary>
    internal static class Profile
    {
        public static byte[] GetProjectBytes()
        {
            try
            {
                MemoryStream stream = new MemoryStream();
                using TextWriter writer = new StreamWriter(stream);

                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                };
                JsonSerializer serializer = JsonSerializer.CreateDefault(settings);

                serializer.Serialize(writer, new Serializer.DevProject());

                writer.Flush();

                var bytes = stream.ToArray();

                stream.Dispose();

                return bytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return Array.Empty<byte>();
        }
        public static string GetProject()
        {
            try
            {
                MemoryStream stream = new MemoryStream();
                using TextWriter writer = new StreamWriter(stream);

                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                };
                JsonSerializer serializer = JsonSerializer.CreateDefault(settings);

                serializer.Serialize(writer, new Serializer.DevProject());

                writer.Flush();

                var json = Encoding.UTF8.GetString(stream.ToArray());

                stream.Dispose();

                return json;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return String.Empty;
        }
        public static string GetContext()
        {
            return File.ReadAllText(Path.Combine(Program.ExecutablePath, Program.IaProfile));
        }
    }
}
