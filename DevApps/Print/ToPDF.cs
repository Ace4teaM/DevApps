using Microsoft.Scripting.Hosting;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.UniversalAccessibility.Drawing;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using static IronPython.Runtime.Profiler;

namespace DevApps.Print
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

        internal static MemoryStream Make(Program.DevFacet facette)
        {
            var stream = new MemoryStream();

            PdfDocument doc = new PdfDocument();
            doc.PageLayout = PdfPageLayout.SinglePage;

            XFont xFont = new XFont("Arial", 20);

            AddFacet(stream, doc, xFont, facette);

            doc.Save(stream);

            return stream;
        }

        internal static MemoryStream Make()
        {
            var stream = new MemoryStream();

            PdfDocument doc = new PdfDocument();
            doc.PageLayout = PdfPageLayout.SinglePage;

            XFont xFont = new XFont("Arial", 20);

            foreach (var facette in Program.DevFacet.References)
            {
                AddFacet(stream, doc, xFont, facette.Value);
            }

            doc.Save(stream);

            return stream;
        }
        public static DrawingVisual CreateDrawingVisual(string key, Program.DevObject o, Rect rect)
        {
            DrawingVisual visual = new DrawingVisual();

            // Commence le dessin avec DrawingContext
            using (DrawingContext dc = visual.RenderOpen())
            {

                // Execute le script de dessin
                if (o.DrawCode.Item2 != null)
                {
                    var handle2 = o.mutexReadOutput.WaitOne();
                    if (handle2)
                    {
                        try
                        {
                            var pyScope = Program.pyEngine.CreateScope();//lock Program.pyEngine !
                            pyScope.SetVariable("out", new DevApps.PythonExtends.Output(o.buildStream, Path.Combine(Program.DataDir, key)));// mise en cache dans l'objet ?
                            pyScope.SetVariable("gui", o.gui);
                            pyScope.SetVariable("name", key);
                            pyScope.SetVariable("dc", dc);
                            pyScope.SetVariable("rect", rect);
                            pyScope.SetVariable("desc", o.Description);

                            foreach (var pointer in o.Pointers)
                            {
                                Program.DevObject.References.TryGetValue(pointer.Value.target, out var pointerRef);
                                pyScope.SetVariable(pointer.Key, new DevApps.PythonExtends.Output(pointerRef != null ? pointerRef.buildStream : new MemoryStream(), Path.Combine(Program.DataDir, key)));// mise en cache dans l'objet ?
                            }

                            o.gui.Begin(dc);
                            o.DrawCode.Item2?.Execute(pyScope);
                            o.gui.End();
                        }
                        catch (Exception ex)
                        {
                            System.Console.WriteLine("******************************************");
                            System.Console.WriteLine("OnRender: " + key);
                            ExceptionOperations eo = Program.pyEngine.GetService<ExceptionOperations>();
                            string error = eo.FormatException(ex);
                            Console.WriteLine(error);
                            System.Console.WriteLine("******************************************");
                        }

                        o.mutexReadOutput.ReleaseMutex();
                    }
                }
            }

            return visual;
        }

        public static XGraphicsPath CreateXGraphicsPathFromGeometryString(string pathData)
        {
            Geometry geometry = Geometry.Parse(pathData);
            PathGeometry flattened = geometry.GetFlattenedPathGeometry(); // aplatit arcs et courbes

            XGraphicsPath xPath = new XGraphicsPath();

            foreach (var figure in flattened.Figures)
            {
                Point start = figure.StartPoint;
                xPath.StartFigure();

                foreach (var segment in figure.Segments)
                {
                    switch (segment)
                    {
                        case LineSegment line:
                            xPath.AddLine(start.X, start.Y, line.Point.X, line.Point.Y);
                            start = line.Point;
                            break;

                        case BezierSegment bezier:
                            xPath.AddBezier(
                                start.X, start.Y,
                                bezier.Point1.X, bezier.Point1.Y,
                                bezier.Point2.X, bezier.Point2.Y,
                                bezier.Point3.X, bezier.Point3.Y);
                            start = bezier.Point3;
                            break;

                        case PolyLineSegment polyLine:
                            foreach (var pt in polyLine.Points)
                            {
                                xPath.AddLine(start.X, start.Y, pt.X, pt.Y);
                                start = pt;
                            }
                            break;

                        case PolyBezierSegment polyBezier:
                            for (int i = 0; i + 2 < polyBezier.Points.Count; i += 3)
                            {
                                xPath.AddBezier(
                                    start.X, start.Y,
                                    polyBezier.Points[i].X, polyBezier.Points[i].Y,
                                    polyBezier.Points[i + 1].X, polyBezier.Points[i + 1].Y,
                                    polyBezier.Points[i + 2].X, polyBezier.Points[i + 2].Y);
                                start = polyBezier.Points[i + 2];
                            }
                            break;

                        // Quadratic et ArcSegments ne sont pas supportés directement
                        // mais sont aplatis dans le PathGeometry via GetFlattenedPathGeometry()
                        // donc dans ce bloc ils ne se présenteront jamais

                        default:
                            throw new NotSupportedException($"Segment type {segment.GetType().Name} not supported.");
                    }
                }

                if (figure.IsClosed)
                {
                    xPath.CloseFigure();
                }
            }

            return xPath;
        }

        public static void DrawVisual(DrawingVisual visual, Rect rect, PdfDocument doc, PdfPage page, XGraphics gfx)
        {
            // Étape 1 : Rendu WPF vers Bitmap
            int width = (int)rect.Width;
            int height = (int)rect.Height;
            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);

            // Étape 2 : Encode en PNG
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                imageBytes = ms.ToArray();
            }

            // Étape 3 : Charge l'image avec System.Drawing
            using var msImage = new MemoryStream(imageBytes);
            using var bitmap = new System.Drawing.Bitmap(msImage);

            // Étape 4 : Création PDF avec PdfSharp
            //var doc = new PdfDocument();
            //var page = doc.AddPage();
            //page.Width = XUnit.FromPoint(width);
            //page.Height = XUnit.FromPoint(height);
            //var gfx = XGraphics.FromPdfPage(page);

            // Étape 5 : Conversion de Bitmap en XImage
            using var imageStream = new MemoryStream();
            bitmap.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
            imageStream.Position = 0;

            var xImage = XImage.FromStream(imageStream);
            gfx.DrawImage(xImage, rect.X, rect.Y, width, height);

            // Sauvegarde finale
            //doc.Save("VisualExport.pdf");
        }
        internal static void AddFacet(MemoryStream stream, PdfDocument doc, XFont xFont, Program.DevFacet facette)
        {
            var layout = facette.PrintLayout;

            PdfPage page = doc.AddPage();
            page.Width = XUnit.FromPoint(layout.Width);
            page.Height = XUnit.FromPoint(layout.Height);

            using (XGraphics gfx = XGraphics.FromPdfPage(page))
            {
                foreach (var obj in facette.Texts)
                {
                    gfx.DrawString(obj.text, new XFont("Verdana", 10), XBrushes.Black, new XPoint(obj.X, obj.Y));
                }

                foreach (var obj in facette.Geometries)
                {
                    XGraphicsPath path = CreateXGraphicsPathFromGeometryString(obj.path);

                    // Sauvegarde l'état graphique actuel
                    gfx.Save();

                    // Applique la translation
                    gfx.TranslateTransform(obj.X, obj.Y);

                    gfx.DrawPath(XPens.Black, null, path);

                    // Restaure l'état graphique précédent (supprime la translation)
                    gfx.Restore();
                }

                foreach (var obj in facette.Objects)
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
                    XRect xRect = new XRect((rect.X - layout.X), (rect.Y - layout.Y), rect.Width, rect.Height);

                    try
                    {
                        switch (contentType)
                        {
                            case ContentType.Undefined:
                                {
                                    DrawingVisual drawingVisual = CreateDrawingVisual(obj.Key, o, new Rect(0, 0, rect.Width, rect.Height));

                                    DrawVisual(drawingVisual, rect, doc, page, gfx);
                                }
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

                                    var fHeight = 1.0 / drawing.Bounds.Height * xRect.Height;

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

                                    XImage image = XImage.FromStream(bitmapStream);
                                    gfx.DrawImage(image, xRect);
                                }
                                break;

                            case ContentType.Text_UTF8:
                                var text = Encoding.UTF8.GetString(content.GetBuffer(), 0, (int)content.Length);

                                gfx.DrawString(text, xFont, XBrushes.Black, xRect, XStringFormats.Center);
                                break;

                            case ContentType.Image_PNG:
                            case ContentType.Image_JPEG:
                                {
                                    XImage image = XImage.FromStream(content);
                                    gfx.DrawImage(image, xRect);
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

            return count == expected_header.Length && header.SequenceEqual(expected_header);
        }
        internal static bool IsBMP(Stream stream)
        {
            var expected_header = new byte[] { 0xF6, 0x04, 0x00, 0x00 };
            var header = new byte[expected_header.Length];
            stream.Seek(0, SeekOrigin.Begin);
            var count = stream.Read(header, 0, expected_header.Length);
            stream.Seek(0, SeekOrigin.Begin);

            return count == expected_header.Length && header.SequenceEqual(expected_header);
        }
        internal static bool IsPNG(Stream stream)
        {
            var expected_header = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            var header = new byte[expected_header.Length];
            stream.Seek(0, SeekOrigin.Begin);
            var count = stream.Read(header, 0, expected_header.Length);
            stream.Seek(0, SeekOrigin.Begin);

            return count == expected_header.Length && header.SequenceEqual(expected_header);
        }
        internal static bool IsJPEG(Stream stream)
        {
            var expected_header = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
            var header = new byte[expected_header.Length];
            stream.Seek(0, SeekOrigin.Begin);
            var count = stream.Read(header, 0, expected_header.Length);
            stream.Seek(0, SeekOrigin.Begin);

            return count == expected_header.Length && header.SequenceEqual(expected_header);
        }
    }
}
