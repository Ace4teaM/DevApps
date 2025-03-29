using DevApps.GUI;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;


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

        internal virtual void level(GUI gui, Output content, string unit, float min, float max, float step)
        {
            var _progress = (1.0 / (max - min)) * (step * content.number());

            // Dessiner le fond de la barre de progression
            Rect backgroundRect = new Rect(0, 0, Width, Height);
            gui.drawingContext?.DrawRectangle(gui.BackgroundBrush, gui.BackgroundPen, backgroundRect);

            // Dessiner la barre de progression
            Rect progressRect = new Rect(0, 0, Width * _progress, Height);
            gui.drawingContext?.DrawRectangle(gui.ForegroundBrush, gui.ForegroundPen, progressRect);

            // Dessiner une bordure
            Pen borderPen = new Pen(Brushes.Black, 2);
            gui.drawingContext?.DrawRectangle(null, gui.BackgroundPen, backgroundRect);
        }
        internal virtual void image(GUI gui, Output data, string format = "auto")
        {
            if(data.bytes().Length == 0)
                return;
            try
            {
                // Créer une instance de BitmapImage
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(data.bytes());
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Charge l'image dans la mémoire
                bitmapImage.EndInit();

                gui.drawingContext?.DrawImage(bitmapImage, new Rect(Top, Left, Right, Bottom));
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }
        }
        internal virtual void text(GUI gui, string text)
        {

        }
        internal virtual void separator(GUI gui)
        {

        }

        internal virtual void list(GUI gui, Output output)
        {

        }

        internal virtual void rectangle(GUI gui, double cornerRadius)
        {
            // Dessiner un rectangle avec des coins arrondis
            Rect rect = new Rect(Left, Top, Width, Height);
            gui.drawingContext?.DrawRoundedRectangle(gui.BackgroundBrush, gui.BackgroundPen, rect, cornerRadius, cornerRadius);
        }
        internal virtual void circle(GUI gui)
        {

        }
        internal virtual void code(GUI gui, string text, string syntax)
        {

        }
        internal virtual void state(GUI gui, Output content, string titleA, string stateA, string titleB, string stateB)
        {
            var _isOn = content.text() == stateA;

            double targetPosition = _isOn ? Width - Height : 0;

            // Fond du bouton
            gui.drawingContext?.DrawRoundedRectangle(gui.BackgroundBrush, gui.BackgroundPen, new Rect(0, 0, Width, Height), 10, 10);

            // Curseur glissant
            gui.drawingContext?.DrawEllipse(gui.ForegroundBrush, gui.ForegroundPen, new Point(targetPosition + Height / 2, Height / 2), Height / 2 - 2, Height / 2 - 2);
        }

        public class Stack : Fill
        {
            public Stack(Zone zone) : base(zone)
            {
            }

            internal override void text(GUI gui, string text)
            {
                double x = Left;
                double y = Top;
                if (String.IsNullOrEmpty(text))
                    return;
                var glyphRun = GUI.ConvertTextToGlyphRun(text, ref x, ref y);
                gui.drawingContext?.DrawGlyphRun(gui.ForegroundBrush, glyphRun);

                Top = y;
            }

            internal override void code(GUI gui, string text, string syntax)
            {
                throw new NotImplementedException();
            }

            internal override void rectangle(GUI gui, double cornerRadius)
            {
                // Création d'un pinceau et d'un stylo
                /* SolidColorBrush fillBrush = new SolidColorBrush(gui.color);
                 Pen borderPen = new Pen(Brushes.Black, cornerRadius);

                 borderPen.Brush = new LinearGradientBrush(
                     GUI.AdjustColorBrightness(gui.color, 1.2),// Teinte plus claire
                     GUI.AdjustColorBrightness(gui.color, 0.8),// Teinte plus sombre
                     new Point(0, 0), // Dégradé du coin supérieur gauche
                     new Point(1, 1)  // vers le coin inférieur droit
                 );*/

                // Dessiner un rectangle avec des coins arrondis
                Rect rect = new Rect(Left, Top, Width, Height);
                gui.drawingContext?.DrawRoundedRectangle(gui.backgroundBrush, gui.backgroundPen, rect, cornerRadius, cornerRadius);

                Top += Height;
            }

            internal override void circle(GUI gui)
            {
                var radiusX = (Right - Left) / 2.0;
                var radiusY = (Bottom - Top) / 2.0;
                gui.drawingContext?.DrawEllipse(gui.ForegroundBrush, null, new Point(Top + radiusX, Left + radiusY), radiusX, radiusY);
            }
            internal override void separator(GUI gui)
            {
                SolidColorBrush fillBrush = new SolidColorBrush(gui.color);
                Pen pen = new Pen(Brushes.Black, 2.0);
                gui.drawingContext?.DrawLine(pen, new Point(Left, Top + 5), new Point(Right, Top + 5));

                Top += 10;
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
                var glyphRun = GUI.ConvertTextToGlyphRun(text, ref x, ref y);
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
                var glyphRun = GUI.ConvertTextToGlyphRun(text, ref x, ref y);
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
        internal bool gradient = false;
        internal Color color = Colors.Black;
        internal double thickness = 2.0;
        internal Pen? foregroundPen;
        internal Brush? foregroundBrush;
        internal Pen? backgroundPen;
        internal Brush? backgroundBrush;

        internal void InvalidateForeground()
        {
            if (gradient)
            {
                foregroundBrush = new LinearGradientBrush(
                    AdjustColorBrightness(color, 1.2),// Teinte plus claire
                    AdjustColorBrightness(color, 0.8),// Teinte plus sombre
                    new Point(0, 0), // Dégradé du coin supérieur gauche
                    new Point(1, 1)  // vers le coin inférieur droit
                );
            }
            else
            {
                foregroundBrush = new SolidColorBrush(color);
            }
            foregroundPen = new Pen(foregroundBrush, thickness);
        }

        internal void InvalidateBackground()
        {
            if (gradient)
            {
                backgroundBrush = new LinearGradientBrush(
                    AdjustColorBrightness(color, 1.2),// Teinte plus claire
                    AdjustColorBrightness(color, 0.8),// Teinte plus sombre
                    new Point(0, 0), // Dégradé du coin supérieur gauche
                    new Point(1, 1)  // vers le coin inférieur droit
                );
            }
            else
            {
                backgroundBrush = new SolidColorBrush(color);
            }
            backgroundPen = new Pen(backgroundBrush, thickness);
        }

        public Pen ForegroundPen { 
            get {
                if(foregroundPen == null)
                {// créé ici dans le thread de l'appelant
                    foregroundBrush = new SolidColorBrush(Colors.LightGray);
                    foregroundPen = new Pen(foregroundBrush, 2.0);
                }
                return foregroundPen;
            }
        }

        public Brush ForegroundBrush
        {
            get
            {
                if (foregroundBrush == null)
                {// créé ici dans le thread de l'appelant
                    foregroundBrush = new SolidColorBrush(Colors.LightGray);
                    foregroundPen = new Pen(foregroundBrush, 2.0);
                }
                return foregroundBrush;
            }
        }

        public Pen BackgroundPen
        {
            get
            {
                if (backgroundPen == null)
                {// créé ici dans le thread de l'appelant
                    backgroundBrush = new SolidColorBrush(Colors.Gray);
                    backgroundPen = new Pen(backgroundBrush, 2.0);
                }
                return backgroundPen;
            }
        }

        public Brush BackgroundBrush
        {
            get
            {
                if (backgroundBrush == null)
                {// créé ici dans le thread de l'appelant
                    backgroundBrush = new SolidColorBrush(Colors.Gray);
                    backgroundPen = new Pen(backgroundBrush, 2.0);
                }
                return backgroundBrush;
            }
        }


        internal DrawingContext? drawingContext;

        public Layout layout { get; set; } = new Layout(new Rect(0, 0, 100, 100));

        internal Zone baseZone = new Zone { Rect = new Rect(0, 0, 100, 100) };
        internal Fill filling { get; set; }
        internal Zone current { get { return zones.First(); } }

        internal Stack<Zone> zones { get; set; }

        public GUI()
        {
            filling = new Fill(baseZone);
            zones = new Stack<Zone>([baseZone]);
        }

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

        /// <summary>
        /// Obtient un texte de l'utilisateur
        /// </summary>
        /// <param name="values"></param>
        public string gettext(Output selection, string? format = null)
        {
            var mousePos = System.Windows.Input.Mouse.GetPosition(null);
            var wnd = new DevApps.GUI.GetText();
            wnd.Value = selection.text();
            if (format != null)
                wnd.Format = new System.Text.RegularExpressions.Regex(format);
            wnd.WindowStartupLocation = WindowStartupLocation.Manual;
            wnd.Left = mousePos.X + 10;
            wnd.Top = mousePos.Y + 10;

            if (wnd.ShowDialog() == true)
            {
                return wnd.Value;
            }

            return selection.text();
        }

        /// <summary>
        /// Obtient un texte de l'utilisateur
        /// </summary>
        /// <param name="values"></param>
        public string getline(Output selection, string? format = null)
        {
            var mousePos = System.Windows.Input.Mouse.GetPosition(null);
            var wnd = new DevApps.GUI.GetText();
            wnd.Value = selection.text();
            wnd.IsMultiline = false;
            if(format != null)
                wnd.Format = new System.Text.RegularExpressions.Regex(format);
            wnd.WindowStartupLocation = WindowStartupLocation.Manual;
            wnd.Left = mousePos.X + 10;
            wnd.Top = mousePos.Y + 10;

            if (wnd.ShowDialog() == true)
            {
                return wnd.Value;
            }

            return selection.text();
        }

        /// <summary>
        /// Edit le contenu 
        /// </summary>
        /// <param name="values"></param>
        public void edit(string editor, Output output)
        {
            // exécute l'environnement de commandes
            try
            {
                // enregistre le contenu dans le fichier si ce n'est pas déjà le cas
                output.Flush();

                var editorName = Service.associatedEditors[editor];
                var editorPath = Service.externalsEditors[editorName];

                var path = Path.GetDirectoryName(editorPath).Replace(@"""","");

                // creation de l'environnement de commandes
                using System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;//System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C \"" + ((editorPath.Contains("%1") == false) ? editorPath + " \"" + Path.GetFullPath(output.Filename) + "\"" : editorPath.Replace("%1", Path.GetFullPath(output.Filename))) + "\"";
                //startInfo.WorkingDirectory = path;
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

                output.Reload();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Obtient une sélection de valeur de l'utilisateur
        /// </summary>
        /// <param name="values"></param>
        public string select(IronPython.Runtime.PythonDictionary values, Output selection)
        {
            var mousePos = System.Windows.Input.Mouse.GetPosition(null);
            var wnd = new DevApps.GUI.Select();
            wnd.Items = values.ToDictionary();
            wnd.WindowStartupLocation = WindowStartupLocation.Manual;
            wnd.Left = mousePos.X + 10;
            wnd.Top = mousePos.Y + 10;

            if (wnd.ShowDialog() == true && wnd.SelectedItem != null)
            {
                return (wnd.SelectedItem as KeyValuePair<object,object>?).Value.Key.ToString();
            }

            return selection.text();
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
        public GUI foreground()
        {
            InvalidateForeground();
            return this;
        }
        public GUI background()
        {
            InvalidateBackground();
            return this;
        }
        public GUI style(byte R, byte G, byte B, double thickness, bool gradient)
        {
            this.color = Color.FromRgb(R, G, B);
            this.thickness = thickness;
            this.gradient = gradient;
            return this;
        }
        public GUI style(string colorName, double thickness, bool gradient)
        {
            System.Drawing.Color systemColor = System.Drawing.Color.FromName(colorName);
            this.color = Color.FromRgb(systemColor.R, systemColor.G, systemColor.B);
            this.thickness = thickness;
            this.gradient = gradient;
            return this;
        }
        public GUI svg(Output output)
        {
            if (output.Stream.Length == 0)
                return this;

            var settings = new WpfDrawingSettings();
            settings.IncludeRuntime = true;
            settings.TextAsGeometry = false;

            var svgReader = new FileSvgReader(settings);
            output.Stream.Seek(0, SeekOrigin.Begin);
            var drawing = svgReader.Read(output.Stream);

            var fHeight = (1.0 / drawing.Bounds.Height) * filling.Height;

            var mx = new Matrix();
            mx.Translate(-drawing.Bounds.X, -drawing.Bounds.Y);
            mx.Scale(fHeight, fHeight);

            drawing.Transform = new MatrixTransform(mx);
            drawingContext?.DrawDrawing(drawing);

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
            filling.level(this, content, unit, min, max, step);
            return this;
        }
        /// <summary>
        /// Dessine un sélecteur à 2 états
        /// </summary>
        public GUI state(Output content, string titleA, string stateA, string titleB, string stateB)
        {
            filling.state(this, content, titleA, stateA, titleB, stateB);
            return this;
        }
        /// <summary>
        /// Dessine une séparation entre l'élément précédent et suivant
        /// </summary>
        public GUI separator()
        {
            filling.separator(this);
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

        internal static GlyphRun ConvertTextToGlyphRun(string line, ref double x, ref double y)
        {
            var glyphIndices = new List<ushort>();
            var advanceWidths = new List<double>();
            var glyphOffsets = new List<Point>();

            for (int j = 0; j < line.Length; ++j)
            {
                ushort glyphIndex = 0;
                try
                {
                    glyphIndex = glyphTypeface.CharacterToGlyphMap[line[j]];
                }
                catch (System.Collections.Generic.KeyNotFoundException ex)
                {
                    var c = line[j];
                    throw new NotImplementedException("Obtenir le glyph depuis un autre Typeface et l'ajouter au cache");
                }

                glyphIndices.Add(glyphIndex);
                advanceWidths.Add(0);
                glyphOffsets.Add(new Point(x, y));

                x += advanceWidth;
            }

            y -= advanceHeight;

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
        internal static GlyphRun ConvertTextLinesToGlyphRun(string[] lines, Layout layout)
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
                    glyphOffsets.Add(new Point(x, y - advanceHeight));

                    x += advanceWidth;

                }

                y += advanceHeight;
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
