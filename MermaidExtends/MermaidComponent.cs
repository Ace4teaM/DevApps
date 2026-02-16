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

        public override async Task<Stream> TryMakeContent(CancellationToken cancellationToken, object input)
        {
            return Stream.Null;
        }

        public override async Task<DrawingVisual> TryMakeRender(CancellationToken cancellationToken, object input, double width)
        {
            var text = input.ToString();
            if (text == null)
                throw new Exception("input text expected");

            var base64 = GenerateBase64(text);

            var url = $"https://mermaid.ink/img/{base64}?type=png";

            using var http = new HttpClient();

            var visual = new DrawingVisual();
            var pngBytes = await http.GetByteArrayAsync(url, cancellationToken);

            using (DrawingContext dc = visual.RenderOpen())
            {
                var bitmap = LoadBitmapFromPngBytes(pngBytes);
                dc.DrawImage(bitmap, new System.Windows.Rect(0, 0, width, (bitmap.Height / bitmap.Width) * width));
            }

            return visual;
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
    }
}
