using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static IronPython.Modules._ast;
using System.Windows.Media.Media3D;

namespace DevApps.PythonExtends
{
    /// <summary>
    /// représente une zone cliente rectangulaire
    /// </summary>
    public class Zone
    {
        public Zone? Previous;

        public Rect Rect;

        public Zone left()
        {
            return new Zone { Rect = new Rect { X = Rect.X, Y = Rect.Y, Width = Rect.Width / 2.0, Height = Rect.Height } };
        }
        public Zone top()
        {
            return new Zone { Rect = new Rect { X = Rect.X, Y = Rect.Y, Width = Rect.Width, Height = Rect.Height / 2.0 } };
        }
        public Zone right()
        {
            return new Zone { Rect = new Rect { X = Rect.X + Rect.Width / 2.0, Y = Rect.Y, Width = Rect.Width / 2.0, Height = Rect.Height } };
        }
        public Zone bottom()
        {
            return new Zone { Rect = new Rect { X = Rect.X, Y = Rect.Y + Rect.Height / 2.0, Width = Rect.Width, Height = Rect.Height / 2.0 } };
        }
        public Zone inflate(double size)
        {
            var z = new Zone { Rect = this.Rect };
            z.Rect.Inflate(size, size);
            return z;
        }
    }

    /// <summary>
    /// représente le positionnement du prochain dessin
    /// </summary>
    public class Fill
    {
        public Zone Base { get; set; }
        /// <summary>
        /// Position en cours
        /// </summary>
        public double Top { get; set; }
        /// <summary>
        /// Position en cours
        /// </summary>
        public double Left { get; set; }
        /// <summary>
        /// Largeur disponible
        /// </summary>
        public double Right { get; set; }
        /// <summary>
        /// Hauteur disponible
        /// </summary>
        public double Bottom { get; set; }
        /// <summary>
        /// Largeur disponible
        /// </summary>
        public double Width { get { return Right - Left; } }
        /// <summary>
        /// Hauteur disponible
        /// </summary>
        public double Height { get { return Bottom - Top; } }

        public Fill(Zone zone)
        {
            this.Base = zone;
            this.Top = zone.Rect.Top;
            this.Left = zone.Rect.Left;
            this.Right = zone.Rect.Right;
            this.Bottom = zone.Rect.Bottom;
        }

        internal virtual void image(GUI gui, Output data, string format = "auto")
        {

        }
        internal virtual void text(GUI gui, string text)
        {

        }
        internal virtual void list(GUI gui, Output output)
        {

        }

        internal virtual void rectangle(GUI gui, double cornerRadius)
        {
            // Création d'un pinceau et d'un stylo
            SolidColorBrush fillBrush = new SolidColorBrush(gui.Foreground);
            Pen borderPen = new Pen(Brushes.Black, cornerRadius);

            borderPen.Brush = new LinearGradientBrush(
                GUI.AdjustColorBrightness(gui.Foreground, 1.2),// Teinte plus claire
                GUI.AdjustColorBrightness(gui.Foreground, 0.8),// Teinte plus sombre
                new Point(0, 0), // Dégradé du coin supérieur gauche
                new Point(1, 1)  // vers le coin inférieur droit
            );
            
            // Dessiner un rectangle avec des coins arrondis
            Rect rect = new Rect(Top, Left, Width, Height);
            gui.drawingContext?.DrawRoundedRectangle(fillBrush, borderPen, rect, cornerRadius, cornerRadius);
        }
        internal virtual void circle(GUI gui)
        {

        }
        internal virtual void code(GUI gui, string text, string syntax)
        {

        }

        public class Stack : Fill
        {
            public double UsedHeight { get; set; }

            public Stack(Zone zone) : base(zone)
            {
                
            }

            internal override void text(GUI gui, string text)
            {
                double x = Left;
                double y = -Top;
                if (String.IsNullOrEmpty(text))
                    return;
                var glyphRun = GUI.ConvertTextToGlyphRun(GUI.glyphTypeface, GUI.renderingEmSize, GUI.advanceWidth, GUI.advanceHeight, GUI.baselineOrigin, text, ref x, ref y);
                gui.drawingContext?.DrawGlyphRun(gui.ForegroundBrush, glyphRun);

                Top = y;

                /*
                Top += UsedHeight;
                Left = Base.Rect.Left;
                AvailableHeight -= UsedHeight;
                */
            }

            internal override void code(GUI gui, string text, string syntax)
            {
                throw new NotImplementedException();
            }

            internal override void rectangle(GUI gui, double cornerRadius)
            {
                throw new NotImplementedException();
            }

            internal override void circle(GUI gui)
            {
                var radiusX = (Right - Left) / 2.0;
                var radiusY = (Bottom - Top) / 2.0;
                gui.drawingContext?.DrawEllipse(gui.ForegroundBrush, null, new Point(Top + radiusX, Left + radiusY), radiusX, radiusY);
            }
            internal override void image(GUI gui, Output data, string format)
            {
                // Créer une instance de BitmapImage
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(data.bytes());
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Charge l'image dans la mémoire
                bitmapImage.EndInit();

                gui.drawingContext?.DrawImage(bitmapImage, new Rect(Top, Left, Right, Bottom));
            }
        }

        public class Stars : Fill
        {
            public int Current { get; set; }
            public int Count { get; set; }
            public double Diameter { get; set; }

            public Stars(Zone zone, int count, double diameter) : base(zone)
            {
                Count = count;
                Diameter = diameter;
            }

            internal override void code(GUI gui, string text, string syntax)
            {
                throw new NotImplementedException();
            }

            internal override void image(GUI gui, Output data, string format)
            {
                throw new NotImplementedException();
            }

            internal override void text(GUI gui, string text)
            {
                throw new NotImplementedException();
            }

            internal override void rectangle(GUI gui, double cornerRadius)
            {
                throw new NotImplementedException();
            }

            internal override void circle(GUI gui)
            {
                throw new NotImplementedException();
            }
        }

        public class Wrap : Fill
        {
            public double UsedWidth { get; set; }
            public double UsedHeight { get; set; }

            public Wrap(Zone zone) : base(zone)
            {

            }

            internal override void code(GUI gui, string text, string syntax)
            {
                throw new NotImplementedException();
            }

            internal override void image(GUI gui, Output data, string format)
            {
                throw new NotImplementedException();
            }

            internal override void text(GUI gui, string text)
            {
                double x = 0;
                double y = 0;
                if (String.IsNullOrEmpty(text))
                    return;
                var glyphRun = GUI.ConvertTextToGlyphRun(GUI.glyphTypeface, GUI.renderingEmSize, GUI.advanceWidth, GUI.advanceHeight, GUI.baselineOrigin, text, ref x, ref y);
                gui.drawingContext?.DrawGlyphRun(gui.ForegroundBrush, glyphRun);
            }

            internal override void rectangle(GUI gui, double cornerRadius)
            {
                throw new NotImplementedException();
            }

            internal override void circle(GUI gui)
            {
                var radiusX = (Right - Left) / 2.0;
                var radiusY = (Bottom - Top) / 2.0;
                gui.drawingContext?.DrawEllipse(gui.ForegroundBrush, null, new Point(Top + radiusX, Left + radiusY), radiusX, radiusY);
            }
        }

        public class Grid : Fill
        {
            public double Columns { get; set; }
            public double Rows { get; set; }
            public double CurrentColumn { get; set; }
            public double CurrentRow { get; set; }

            public Grid(Zone zone, double columns, double rows) : base(zone)
            {
                Columns = columns;
                Rows = rows;
            }

            internal override void code(GUI gui, string text, string syntax)
            {
                throw new NotImplementedException();
            }

            internal override void image(GUI gui, Output data, string format)
            {
                throw new NotImplementedException();
            }

            internal override void text(GUI gui, string text)
            {
                double x = Left;
                double y = Top;
                if (String.IsNullOrEmpty(text))
                    return;
                var glyphRun = GUI.ConvertTextToGlyphRun(GUI.glyphTypeface, GUI.renderingEmSize, GUI.advanceWidth, GUI.advanceHeight, GUI.baselineOrigin, text, ref x, ref y);
                gui.drawingContext?.DrawGlyphRun(gui.ForegroundBrush, glyphRun);

                CurrentColumn++;
                if (CurrentColumn >= Columns)
                {
                    CurrentColumn = 0;
                    CurrentRow++;

                    Left = this.Base.Rect.Left;
                    Top = y;
                }
                else
                {
                    Left = x + 10;//todo gérer la largeur de colonne
                }
            }

            internal override void rectangle(GUI gui, double cornerRadius)
            {
                throw new NotImplementedException();
            }

            internal override void circle(GUI gui)
            {
                throw new NotImplementedException();
            }
        }
    }

    /// <summary>
    /// Graphic object interface
    /// </summary>
    public class GUI
    {
        public Brush GetBackground() { return BackgroundBrush; }

        internal Brush BackgroundBrush = new SolidColorBrush(Colors.LightGray);
        internal Brush ForegroundBrush { get { return new SolidColorBrush(Foreground); } }    
        internal Color Foreground = Colors.Black;

        internal DrawingContext? drawingContext;

        public Layout layout { get; set; } = new Layout(new Rect(0, 0, 100, 100));

        internal static Zone baseZone = new Zone { Rect = new Rect(0, 0, 100, 100) };
        internal Fill filling { get; set; } = new Fill(baseZone);
        internal Zone current { get { return zones.First(); } }

        internal Stack<Zone> zones = new Stack<Zone>([baseZone]);

        // Méthode pour limiter les valeurs entre min et max
        internal static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        // Méthode pour ajuster la luminosité d'une couleur
        internal static Color AdjustColorBrightness(Color baseColor, double factor)
        {
            // Factor > 1.0 pour rendre la couleur plus claire, < 1.0 pour plus sombre
            byte r = (byte)Clamp(baseColor.R * factor, 0, 255);
            byte g = (byte)Clamp(baseColor.G * factor, 0, 255);
            byte b = (byte)Clamp(baseColor.B * factor, 0, 255);
            return Color.FromRgb(r, g, b);
        }

        internal void Begin(DrawingContext context)
        {
            drawingContext = context;
            fill();
            full();
        }

        internal void End()
        {
            drawingContext = null;
        }

        #region zoning
        public GUI left()
        {
            zones.Push(current.left());
            return this;
        }
        public GUI top()
        {
            zones.Push(current.top());
            return this;
        }
        public GUI right()
        {
            zones.Push(current.right());
            return this;
        }
        public GUI bottom()
        {
            zones.Push(current.bottom());
            return this;
        }
        public GUI inflate(double size)
        {
            zones.Push(current.inflate(size));
            return this;
        }
        public GUI full()
        {
            zones.Clear();
            zones.Push(baseZone);
            return this;
        }
        #endregion

        #region filling
        public GUI fill()
        {
            filling = new Fill(current);
            return this;
        }
        public GUI stack()
        {
            filling = new Fill.Stack(current);
            return this;
        }
        public GUI grid(int rows, int cols)
        {
            filling = new Fill.Grid(current, cols, rows);
            return this;
        }
        public GUI wrap()
        {
            filling = new Fill.Wrap(current);
            return this;
        }
        public GUI stars(int count, double diameter)
        {
            filling = new Fill.Stars(current, count, diameter);
            return this;
        }
        public GUI pop()
        {
            if(zones.Count > 1)
                zones.Pop();
            return this;
        }
        #endregion
        #region painting
        public GUI foreground(byte R, byte G, byte B)
        {
            Foreground = Color.FromRgb(R, G, B);
            return this;
        }
        public GUI gray()
        {
            Foreground = Colors.Gray;
            return this;
        }
        public GUI white()
        {
            Foreground = Colors.White;
            return this;
        }
        public GUI text()
        {
            return this;
        }
        public GUI list(Output output)
        {
            filling.list(this, output);
            return this;
        }
        /// <summary>
        /// Dessine un sélecteur numérique
        /// </summary>
        public GUI level(Output content, string unit, float min, float max, float step)
        {
            //fill.level(this, content, unit, min, max, step);
            return this;
        }
        /// <summary>
        /// Dessine un sélecteur à 2 états
        /// </summary>
        public GUI state(Output content, string titleA, string stateA, string titleB, string stateB)
        {
            //fill.state(this, content, titleA, stateA, titleB, stateB);
            return this;
        }
        /// <summary>
        /// Dessine une séparation entre l'élément précédent et suivant
        /// </summary>
        public GUI separator()
        {
            //fill.separator(this);
            return this;
        }
        /// <summary>
        /// Permet d'éditer un contenu
        /// </summary>
        public GUI edit(Output content)
        {
            //fill.edit(this, content);
            return this;
        }
        /// <summary>
        /// Dessine une icone connue avec un font transparent et une couleur d'avant plan
        /// </summary>
        public GUI icon(string name)
        {
            int code = 0;
            switch(name)
            {
                case "user":
                    code = Char.ConvertToUtf32("👤",0);
                    break;
                case "lock":
                    code = Char.ConvertToUtf32("🔒", 0);
                    break;
                case "unlock":
                    code = Char.ConvertToUtf32("🔓", 0);
                    break;
                case "key":
                    code = Char.ConvertToUtf32("🔐", 0);
                    break;
                case "left":
                    code = Char.ConvertToUtf32("←", 0);
                    break;
                case "right":
                    code = Char.ConvertToUtf32("→", 0);
                    break;
                case "up":
                    code = Char.ConvertToUtf32("↑", 0);
                    break;
                case "down":
                    code = Char.ConvertToUtf32("↓", 0);
                    break;
                case "left right":
                    code = Char.ConvertToUtf32("⬌", 0);
                    break;
                case "up down":
                    code = Char.ConvertToUtf32("⬍", 0);
                    break;
                case "gear":
                    code = Char.ConvertToUtf32("⚙", 0);
                    break;
            }
            //todo draw text
            return this;
        }
        public GUI rectangle(float cornerRadius = 0.0f)
        {
            filling.rectangle(this, cornerRadius);

            return this;
        }
        public GUI circle()
        {
            filling.circle(this);

            return this;
        }
        public GUI text(byte[] bytes, int encoding = 65001)
        {
            if (bytes.Length == 0)
                return this;
            var en = Encoding.GetEncoding(encoding);
            var text = en.GetString(bytes);

            filling.text(this, text);

            return this;
        }
        public GUI text(string text)
        {
            if (String.IsNullOrEmpty(text))
                return this;

            filling.text(this, text);

            return this;
        }
        public GUI text(string text, string syntax)
        {
            if (String.IsNullOrEmpty(text))
                return this;

            filling.code(this, text, syntax);

            return this;
        }
        public GUI text(string[] texts)
        {
            if (texts.Length == 0)
                return this;

            foreach (var text in texts)
            {
                filling.text(this, text);
            }
            return this;
        }
        public GUI image(Output data, string format = "auto")
        {
            filling.image(this, data, format);

            return this;
        }
        /* public void grid(string[] lines, string columnsExp)
         {
             if (lines.Length == 0)
                 return;

             var reg = new Regex(columnsExp, RegexOptions.IgnoreCase);

             var results = lines.Select(p => reg.Match(p)).ToArray();

             var columns = results.Select(p => p.Groups.Values.Count() - 1).Max();

             x = 10;
             double xMax = 0;
             for (int i = 0; i < columns; i++)
             {
                 double yy = y;
                 for (int j = 0; j < results.Length; j++)
                 {
                     double xx = x;
                     var text = results[j].Groups[1 + i].Value;
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
        */
        #endregion

        internal static GlyphTypeface glyphTypeface;
        internal static double renderingEmSize, advanceWidth, advanceHeight;
        internal static Point baselineOrigin;

        static GUI()
        {

            new Typeface("Consolas").TryGetGlyphTypeface(out glyphTypeface);
            renderingEmSize = 10;
            advanceWidth = glyphTypeface.AdvanceWidths[0] * renderingEmSize;
            advanceHeight = glyphTypeface.Height * renderingEmSize;
            baselineOrigin = new Point(0, glyphTypeface.Baseline * renderingEmSize);

        }

        internal static GlyphRun ConvertTextToGlyphRun(GlyphTypeface glyphTypeface, double renderingEmSize, double advanceWidth, double advanceHeight, Point baselineOrigin, string line, ref double x, ref double y)
        {
            var glyphIndices = new List<ushort>();
            var advanceWidths = new List<double>();
            var glyphOffsets = new List<Point>();

            for (int j = 0; j < line.Length; ++j)
            {
                var glyphIndex = glyphTypeface.CharacterToGlyphMap[line[j]];
                glyphIndices.Add(glyphIndex);
                advanceWidths.Add(0);
                glyphOffsets.Add(new Point(x, y));

                x += advanceWidth;

            }
            y -= baselineOrigin.Y;

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
        internal static GlyphRun ConvertTextLinesToGlyphRun(GlyphTypeface glyphTypeface, double renderingEmSize, double advanceWidth, double advanceHeight, Point baselineOrigin, string[] lines, Layout layout)
        {
            var glyphIndices = new List<ushort>();
            var advanceWidths = new List<double>();
            var glyphOffsets = new List<Point>();

            double x = layout.CurrentRect.X;
            double y = layout.CurrentRect.Y;

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
