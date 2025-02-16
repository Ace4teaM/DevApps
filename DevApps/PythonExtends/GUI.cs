using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DevApps.PythonExtends
{
    /// <summary>
    /// Graphic object interface
    /// </summary>
    public class GUI
    {
        internal double x;
        internal double y;
        internal double w = 100;
        internal double h = 100;

        internal DrawingContext? drawingContext;

        public class Shape
        {
            public Shape fill()
            {
                return this;
            }
        }
        public class Layout
        {
            public Layout fill()
            {
                return this;
            }
            public Layout stack()
            {
                return this;
            }
            public Layout top(float height)
            {
                return this;
            }
            public Layout marginLeft(float marging)
            {
                return this;
            }
            public Layout copy()
            {
                return new Layout();
            }
        }

        public Layout layout { get; set; } = new Layout();

        internal void Begin(DrawingContext context)
        {
            drawingContext = context;
            x = 0;
            y = 10;
        }

        internal void End()
        {
            drawingContext = null;
        }

        public Shape rectangle()
        {
            return new Shape();
        }

        static GlyphTypeface glyphTypeface;
        static double renderingEmSize, advanceWidth, advanceHeight;
        static Point baselineOrigin;

        static GUI()
        {

            new Typeface("Consolas").TryGetGlyphTypeface(out glyphTypeface);
            renderingEmSize = 10;
            advanceWidth = glyphTypeface.AdvanceWidths[0] * renderingEmSize;
            advanceHeight = glyphTypeface.Height * renderingEmSize;
            baselineOrigin = new Point(0, glyphTypeface.Baseline * renderingEmSize);

        }

        public void text(byte[] bytes, int encoding = 65001)
        {
            if (bytes.Length == 0)
                return;
            var en = Encoding.GetEncoding(encoding);
            var text = en.GetString(bytes);

            var glyphRun = ConvertTextLinesToGlyphRun(glyphTypeface, renderingEmSize, advanceWidth, advanceHeight, baselineOrigin, text.Split('\n','\r'), ref x, ref y);
            drawingContext?.DrawGlyphRun(Brushes.Black, glyphRun);
        }
        public void text(string text)
        {
            if (String.IsNullOrEmpty(text))
                return;
            var glyphRun = ConvertTextLinesToGlyphRun(glyphTypeface, renderingEmSize, advanceWidth, advanceHeight, baselineOrigin, text.Split('\n', '\r'), ref x, ref y);
            drawingContext?.DrawGlyphRun(Brushes.Black, glyphRun);
        }
        public void line(string text)
        {
            if (String.IsNullOrEmpty(text))
                return;
            var glyphRun = ConvertTextToGlyphRun(glyphTypeface, renderingEmSize, advanceWidth, advanceHeight, baselineOrigin, text, ref x, ref y);
            drawingContext?.DrawGlyphRun(Brushes.Black, glyphRun);
            x = 0;
        }
        public void lines(string[] lines)
        {
            if (lines.Length == 0)
                return;
            double yy = y;

            foreach (var line in lines)
            {
                double xx = x;
                if (String.IsNullOrEmpty(line))
                    continue;
                var glyphRun = ConvertTextToGlyphRun(glyphTypeface, renderingEmSize, advanceWidth, advanceHeight, baselineOrigin, line, ref xx, ref yy);
                drawingContext?.DrawGlyphRun(Brushes.Black, glyphRun);
            }
        }
        public void word(string text)
        {
            if (String.IsNullOrEmpty(text))
                return;
            var glyphRun = ConvertTextToGlyphRun(glyphTypeface, renderingEmSize, advanceWidth, advanceHeight, baselineOrigin, text, ref x, ref y);
            drawingContext?.DrawGlyphRun(Brushes.Black, glyphRun);
        }
        public void grid(string[] lines, string columnsExp)
        {
            if (lines.Length == 0)
                return;

            var reg = new Regex(columnsExp, RegexOptions.IgnoreCase);

            var results = lines.Select(p=> reg.Match(p)).ToArray();

            var columns = results.Select(p=>p.Groups.Values.Count()-1).Max();

            x = 10;
            double xMax = 0;
            for (int i = 0; i < columns; i++)
            {
                double yy = y;
                for (int j = 0; j < results.Length; j++)
                {
                    double xx = x;
                    var text = results[j].Groups[1+i].Value;
                    if (String.IsNullOrEmpty(text))
                        continue;
                    var glyphRun = ConvertTextToGlyphRun(glyphTypeface, renderingEmSize, advanceWidth, advanceHeight, baselineOrigin, text, ref xx, ref yy);
                    drawingContext?.DrawGlyphRun(Brushes.Black, glyphRun);

                    xMax = Math.Max(xMax, xx);
                    y += 10;
                }

                x = xMax + 10;
                y = 10;
            }
        }
        public void code(string text, string syntax)
        {
            throw new NotImplementedException();
        }
        public Shape ellipse()
        {
            return new Shape();
        }
        public Shape image(Output data, string format = "auto")
        {
            // Créer une instance de BitmapImage
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(data.bytes());
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Charge l'image dans la mémoire
            bitmapImage.EndInit();

            drawingContext?.DrawImage(bitmapImage, new Rect(0, 0, w, h));

            return new Shape();
        }
        static GlyphRun ConvertTextToGlyphRun(GlyphTypeface glyphTypeface, double renderingEmSize, double advanceWidth, double advanceHeight, Point baselineOrigin, string line, ref double x, ref double y)
        {
            var glyphIndices = new List<ushort>();
            var advanceWidths = new List<double>();
            var glyphOffsets = new List<Point>();

            y -= baselineOrigin.Y;
            for (int j = 0; j < line.Length; ++j)
            {
                var glyphIndex = glyphTypeface.CharacterToGlyphMap[line[j]];
                glyphIndices.Add(glyphIndex);
                advanceWidths.Add(0);
                glyphOffsets.Add(new Point(x, y));

                x += advanceWidth;

            }

            return new GlyphRun(
                glyphTypeface,
                0,
                false,
                renderingEmSize,
                glyphIndices,
                baselineOrigin,
                advanceWidths,
                glyphOffsets,
                null,
                null,
                null,
                null,
                null);
        }
        static GlyphRun ConvertTextLinesToGlyphRun(GlyphTypeface glyphTypeface, double renderingEmSize, double advanceWidth, double advanceHeight, Point baselineOrigin, string[] lines, ref double x, ref double y)
        {
            var glyphIndices = new List<ushort>();
            var advanceWidths = new List<double>();
            var glyphOffsets = new List<Point>();

            y -= baselineOrigin.Y;
            for (int i = 0; i < lines.Length; ++i)
            {
                var line = lines[i];

                x = baselineOrigin.X;
                for (int j = 0; j < line.Length; ++j)
                {
                    var glyphIndex = glyphTypeface.CharacterToGlyphMap[line[j]];
                    glyphIndices.Add(glyphIndex);
                    advanceWidths.Add(0);
                    glyphOffsets.Add(new Point(x, y));

                    x += advanceWidth;

                }

                y -= advanceHeight;
            }

            return new GlyphRun(
                glyphTypeface,
                0,
                false,
                renderingEmSize,
                glyphIndices,
                baselineOrigin,
                advanceWidths,
                glyphOffsets,
                null,
                null,
                null,
                null,
                null);
        }
    }
}
