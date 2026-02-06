using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DevApps.Extends
{
    public sealed class MermaidComponent : ExtendedComponent
    {
        internal static Dictionary<string, byte[]> database = new();

        public MermaidComponent()
        {
        }

        public override void Dispose(){ }

        public override void SetVariable(string name, object value) { }

        public override bool TryMakeVariable(object input, out object? variable)
        {
            variable = null;
            return true;
        }

        public override bool TryMakeRender(object input, double width, DrawingContext drawing)
        {
            try
            {
                var text = input.ToString();
                var base64 = GenerateBase64(text);

                if (database.TryGetValue(base64, out var pngBytes) == false)
                {
                    var task = GeneratePngAsync(base64);
                    if (task.Wait(6000))
                    {
                        pngBytes = task.Result;
                        database[base64] = pngBytes;
                    }
                }
                
                var bitmap = LoadBitmapFromPngBytes(pngBytes);
                drawing.DrawImage(bitmap, new System.Windows.Rect(0, 0, width, (bitmap.Height / bitmap.Width) * width));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        internal static string GenerateBase64(string mermaidCode)
        {
            // encode en base64 (UTF8 → base64 → url-safe)
            var bytes = Encoding.UTF8.GetBytes(mermaidCode);
            var base64 = Convert.ToBase64String(bytes);

            // mermaid.ink attend base64 URL-safe
            return base64
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
        internal static async Task<byte[]> GeneratePngAsync(string base64)
        {
            var url = $"https://mermaid.ink/img/{base64}?type=png";

            using var http = new HttpClient();
            var pngBytes = await http.GetByteArrayAsync(url);

            return pngBytes;
        }
        internal static BitmapImage LoadBitmapFromPngBytes(byte[] pngBytes)
        {
            using var ms = new MemoryStream(pngBytes);

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad; // charge tout en mémoire
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze(); // important : perf + thread-safe

            return bmp;
        }
    }
}
