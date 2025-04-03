using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DevApps
{
    internal class ToPDF
    {
        internal enum ContentType
        {
            Undefined,
            Text_UTF8,
            Image_PNG,
            Image_JPEG,
            Image_SVG
        }

        internal static MemoryStream Make()
        {
            var stream = new MemoryStream();

            PdfDocument doc = new PdfDocument();
            doc.PageLayout = PdfPageLayout.SinglePage;

            XFont xFont = new XFont("Arial", 20);

            foreach (var facette in Program.DevFacet.References)
            {
                var layout = facette.Value.GetZone();

                double margin = 10;

                PdfPage page = doc.AddPage();
                page.Width = XUnit.FromPoint(layout.Width + margin * 2);
                page.Height = XUnit.FromPoint(layout.Height + margin * 2);

                foreach (var obj in facette.Value.Objects)
                {
                    ContentType contentType = ContentType.Undefined;

                    var o = Program.DevObject.Get(obj.Key);

                    if (o == null)
                        continue;

                    var content = o.buildStream;

                    if (contentType == ContentType.Undefined && IsJPEG(content))
                    {
                        contentType = ContentType.Image_JPEG;
                    }

                    if (contentType == ContentType.Undefined && IsPNG(content))
                    {
                        contentType = ContentType.Image_PNG;
                    }

                    if (contentType == ContentType.Undefined && IsSVG(content))
                    {
                        contentType = ContentType.Image_SVG;
                    }

                    if (contentType == ContentType.Undefined && IsUTF8(content))
                    {
                        contentType = ContentType.Text_UTF8;
                    }

                    var rect = obj.Value.GetZone();
                    XRect xRect = new XRect(margin + (rect.X - layout.X), margin + (rect.Y - layout.Y), rect.Width, rect.Height);

                    try
                    {
                        switch (contentType)
                        {
                            case ContentType.Undefined:
                                Console.WriteLine("Can't detect content type !");
                                break;

                            case ContentType.Image_SVG:
                                {
                                    DrawingVisual drawingVisual = new DrawingVisual();
                                    using DrawingContext drawingContext = drawingVisual.RenderOpen();

                                    var settings = new WpfDrawingSettings();
                                    settings.IncludeRuntime = true;
                                    settings.TextAsGeometry = false;

                                    var svgReader = new FileSvgReader(settings);
                                    content.Seek(0, SeekOrigin.Begin);
                                    var drawing = svgReader.Read(content);

                                    var fHeight = (1.0 / drawing.Bounds.Height) * xRect.Height;

                                    var mx = new Matrix();
                                    mx.Translate(-drawing.Bounds.X, -drawing.Bounds.Y);
                                    mx.Scale(fHeight, fHeight);

                                    drawing.Transform = new MatrixTransform(mx);
                                    drawingContext.DrawDrawing(drawing);
                                    drawingContext.Close();

                                    RenderTargetBitmap bmp = new RenderTargetBitmap((int)xRect.Width, (int)xRect.Height, 120, 96, PixelFormats.Pbgra32);
                                    bmp.Render(drawingVisual);

                                    MemoryStream bitmapStream = new MemoryStream();
                                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(bmp));
                                    encoder.Save(bitmapStream);

                                    using (XGraphics g = XGraphics.FromPdfPage(page))
                                    {
                                        XImage image = XImage.FromStream(bitmapStream);
                                        g.DrawImage(image, xRect);
                                    }
                                }
                                break;

                            case ContentType.Text_UTF8:
                                var text = Encoding.UTF8.GetString(content.GetBuffer(), 0, (int)content.Length);

                                using (XGraphics g = XGraphics.FromPdfPage(page))
                                {
                                    g.DrawString(text, xFont, XBrushes.Black, xRect, XStringFormats.Center);
                                }
                                break;

                            case ContentType.Image_PNG:
                            case ContentType.Image_JPEG:
                                using (XGraphics g = XGraphics.FromPdfPage(page))
                                {
                                    XImage image = XImage.FromStream(content);
                                    g.DrawImage(image, xRect);
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            doc.Save(stream);

            return stream;
        }

        internal static bool IsSVG(Stream stream)
        {
            char[] block = new char[1024];
            TextReader textReader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
            stream.Seek(0, SeekOrigin.Begin);
            var count = textReader.ReadBlock(block, 0, 1024);
            var i = 0;
            while (i < count - 4)
            {
                if (block[i] == '<' && block[i + 1] == 's' && block[i + 2] == 'v' && block[i + 3] == 'g')
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    return true;
                }

                i++;
            }

            stream.Seek(0, SeekOrigin.Begin);
            return false;
        }
        internal static bool IsUTF8(Stream stream)
        {
            var expected_header = new byte[] { 0xEF, 0xBB, 0xBF };
            var header = new byte[expected_header.Length];
            var count = stream.Read(header, 0, expected_header.Length);
            stream.Seek(0, SeekOrigin.Begin);

            return (count == expected_header.Length && header.SequenceEqual(expected_header));
        }
        internal static bool IsPNG(Stream stream)
        {
            var expected_header = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            var header = new byte[expected_header.Length];
            stream.Seek(0, SeekOrigin.Begin);
            var count = stream.Read(header, 0, expected_header.Length);
            stream.Seek(0, SeekOrigin.Begin);

            return (count == expected_header.Length && header.SequenceEqual(expected_header));
        }
        internal static bool IsJPEG(Stream stream)
        {
            var expected_header = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
            var header = new byte[expected_header.Length];
            stream.Seek(0, SeekOrigin.Begin);
            var count = stream.Read(header, 0, expected_header.Length);
            stream.Seek(0, SeekOrigin.Begin);

            return (count == expected_header.Length && header.SequenceEqual(expected_header));
        }
    }
}
