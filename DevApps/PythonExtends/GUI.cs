using CsvHelper;
using CsvHelper.Configuration;
using DevApps.GUI;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

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
    /// Fournit les méthodes de dessins au lanage python pour les objets les plus fréquents (csv,svg,textes,...)
    /// </summary>
    public class GUI
    {
        /// <summary>
        /// Texte pour les messages d'erreurs et autres
        /// </summary>
        internal double TextEmSize = 16.0;

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

        /// <summary>
        /// Contexte de dessin à utiliser pour cette instance
        /// Chaque objet possède sa propre instance de la classe GUI
        /// </summary>
        internal DrawingContext? drawingContext;

        public GUI()
        {
            Top = 0;//todo récupération depuis constructeur
            Left = 0;
            Right = 100;
            Bottom = 100;
        }

        /// <summary>
        /// Début du dessin
        /// </summary>
        /// <remarks>
        /// context doit déjà être ouvert et prêt au dessin
        /// </remarks>
        internal void Begin(DrawingContext context)
        {
            drawingContext = context;
        }

        /// <summary>
        /// Fin du dessin
        /// </summary>
        internal void End()
        {
            drawingContext = null;
        }

        /// <summary>
        /// Méthode Python : Ouvre une boite de dialogue et obtient un texte de l'utilisateur
        /// </summary>
        /// <returns>
        /// Retourne la valeur sélectionné ou la valeur de base si l'utilisateur annule
        /// </returns>
        /// <param name="selection">Valeur de base</param>
        /// <param name="format">Regex de validation de la valeur, si null aucune vérification</param>
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
        /// Méthode Python : Obtient un texte de l'utilisateur (sans passage de ligne)
        /// </summary>
        /// <returns>
        /// Retourne la valeur sélectionné ou la valeur de base si l'utilisateur annule
        /// </returns>
        /// <param name="selection">Valeur de base</param>
        /// <param name="format">Regex de validation de la valeur, si null aucune vérification</param>
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
        /// Méthode Python : Edite les données du contenu 
        /// </summary>
        /// <remarks>
        /// Ouvre un processus enfant sur l'éditeur et attend la fermeture de ce dernier pour mettre à jour les données
        /// Le fichier à modifier est inséré à la place de "%1" dans le chemin vers l'éditeur ou à la fin si introuvable
        /// </remarks>
        /// <param name="editor">Nom de l'éditeur tel que définit dans la liste des éditeurs externes</param>
        /// <param name="output">Données à éditer</param>
        public void edit(string editor, Output output)
        {
            // exécute l'environnement de commandes
            try
            {
                // enregistre le contenu dans le fichier si ce n'est pas déjà le cas
                output.Flush();

                var editorName = GuiService.associatedEditors[editor];
                var editorPath = GuiService.externalsEditors[editorName];

                var path = Path.GetDirectoryName(editorPath)?.Replace(@"""","");

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
                System.Console.WriteLine("edit: Echec de l'ouverture de l'éditeur");
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

            if (wnd.ShowDialog() == true && wnd.SelectedItem is KeyValuePair<object, object> sel)
            {
                return sel.Key.ToString() ?? String.Empty;
            }

            return selection.text();
        }

        public class Row
        {
            public string? A, B, C, D, E, F, G, H, I, J;
            public string? Get(int index)
            {
                switch (index)
                {
                    case 0: return A;
                    case 1: return B;
                    case 2: return C;
                    case 3: return D;
                    case 4: return E;
                    case 5: return F;
                    case 6: return G;
                    case 7: return H;
                    case 8: return I;
                    case 9: return J;
                }
                return null;
            }
        }

        public GUI csv(Output output, bool header, string? delimiter = null)
        {
            try
            {
                output.Stream.Seek(0, SeekOrigin.Begin);
                DataTable dataTable = new DataTable();

                var config = new CsvConfiguration(CultureInfo.InvariantCulture);
                config.ReadingExceptionOccurred = re =>
                {
                    // HERE YOU CAN DO ANYTHING YOU WANT WITH A BAD ROW
                    Debug.WriteLine($"Bad Row '; CSV ERROR: {re.Exception}");
                    return false; // <-- tells process to continue
                };
                config.DetectDelimiter = delimiter == null;
                config.BadDataFound = null;
                config.IgnoreBlankLines = true;
                config.TrimOptions = TrimOptions.Trim;
                if (String.IsNullOrEmpty(delimiter) == false)
                    config.Delimiter = delimiter;

                using (var csv = new CsvReader(new StreamReader(output.Stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true), config))
                {
                    if (csv != null)
                    {
                        double MarginY = 0;
                        double MarginX = 0;

                        int colCount = 0;
                        int rowCount = 0;

                        var dc = drawingContext;

                        var height = 0.0;
                        List<Row> rows = new List<Row>();
                        csv.Read();

                        if (header)
                        {
                            csv.ReadHeader();
                        }

                        while (csv.Read() && height < Height)
                        {
                            colCount = Math.Max(csv.ColumnCount, colCount);

                            switch (csv.ColumnCount)
                            {
                                case 0:
                                    rows.Add(new Row());
                                    break;
                                case 1:
                                    rows.Add(new Row { A = csv.GetField(0) });
                                    break;
                                case 2:
                                    rows.Add(new Row { A = csv.GetField(0), B = csv.GetField(1) });
                                    break;
                                case 3:
                                    rows.Add(new Row { A = csv.GetField(0), B = csv.GetField(1), C = csv.GetField(2) });
                                    break;
                                case 4:
                                    rows.Add(new Row { A = csv.GetField(0), B = csv.GetField(1), C = csv.GetField(2), D = csv.GetField(3) });
                                    break;
                                case 5:
                                    rows.Add(new Row { A = csv.GetField(0), B = csv.GetField(1), C = csv.GetField(2), D = csv.GetField(3), E = csv.GetField(4) });
                                    break;
                                case 6:
                                    rows.Add(new Row { A = csv.GetField(0), B = csv.GetField(1), C = csv.GetField(2), D = csv.GetField(3), E = csv.GetField(4), F = csv.GetField(5) });
                                    break;
                                case 7:
                                    rows.Add(new Row { A = csv.GetField(0), B = csv.GetField(1), C = csv.GetField(2), D = csv.GetField(3), E = csv.GetField(4), F = csv.GetField(5), G = csv.GetField(6) });
                                    break;
                                case 8:
                                    rows.Add(new Row { A = csv.GetField(0), B = csv.GetField(1), C = csv.GetField(2), D = csv.GetField(3), E = csv.GetField(4), F = csv.GetField(5), G = csv.GetField(6), H = csv.GetField(7) });
                                    break;
                                case 9:
                                    rows.Add(new Row { A = csv.GetField(0), B = csv.GetField(1), C = csv.GetField(2), D = csv.GetField(3), E = csv.GetField(4), F = csv.GetField(5), G = csv.GetField(6), H = csv.GetField(7), I = csv.GetField(8) });
                                    break;
                            }

                            height += GlyphCache.CachedGlyphTypeface.Height;
                            rowCount++;
                        }

                        // Calcul des largeurs de colonnes
                        double[] colWidths = new double[colCount];
                        double maxRowHeight = 0;
                        double minWidth = 40.0;

                        GlyphRun[,] glyphs = new GlyphRun[rowCount, colCount];

                        for (int col = 0; col < colCount; col++)
                        {
                            double maxWidth = 0;
                            for (int row = 0; row < rowCount; row++)
                            {
                                var text = rows[row].Get(col);
                                if (text != null)
                                {
                                    var glyph = GlyphCache.CreateGlyphRun(text, TextEmSize, new Point(0, 0));
                                    var box = glyph.ComputeAlignmentBox();
                                    maxWidth = Math.Max(maxWidth, box.Width + 8); // Padding
                                    maxWidth = Math.Max(maxWidth, minWidth); // Minimum
                                    maxRowHeight = Math.Max(maxRowHeight, box.Height + 8);
                                    glyphs[row, col] = glyph;
                                }
                            }
                            colWidths[col] = maxWidth;
                        }

                        if (header && csv.HeaderRecord != null)
                        {
                            double x = MarginX + (minWidth / 3.0) + 0.5;

                            for (int col = 0; col < colCount; col++)
                            {
                                var glyph = GlyphCache.CreateGlyphRun(csv.HeaderRecord[col] ?? "", TextEmSize, new Point(0, 0));

                                if (glyph.ComputeAlignmentBox().Width > colWidths[col])
                                {
                                    dc?.PushTransform(new TranslateTransform(x, MarginY - 6));
                                    dc?.PushTransform(new RotateTransform(-45, 0, 0));
                                    dc?.DrawGlyphRun(Brushes.DarkBlue, glyph);
                                    dc?.Pop();
                                    dc?.Pop();
                                }
                                else
                                {
                                    dc?.PushTransform(new TranslateTransform(x, MarginY - 6));
                                    dc?.DrawGlyphRun(Brushes.DarkBlue, glyph);
                                    dc?.Pop();
                                }

                                x += colWidths[col];
                            }
                        }

                        double y = MarginY + 0.5;

                        for (int row = 0; row < rowCount; row++)
                        {
                            double x = MarginX + 0.5;

                            for (int col = 0; col < colCount; col++)
                            {
                                var text = rows[row].Get(col);
                                double cellWidth = colWidths[col];
                                double cellHeight = maxRowHeight;

                                // bordure de la cellule
                                dc?.DrawRectangle(null, new Pen(Brushes.Black, 1), new Rect(x, y, cellWidth, cellHeight));

                                // texte de la cellule
                                var glyphRun = glyphs[row, col];
                                if (glyphRun != null)
                                {
                                    var box = glyphRun.ComputeAlignmentBox();
                                    var textPos = new Point(x + 4, y + (cellHeight - box.Height) / 2 - box.Top); // Center vertically
                                    var drawGlyph = new GlyphRun(
                                        glyphRun.GlyphTypeface,
                                        glyphRun.BidiLevel,
                                        glyphRun.IsSideways,
                                        glyphRun.FontRenderingEmSize,
                                        glyphRun.PixelsPerDip,
                                        glyphRun.GlyphIndices,
                                        textPos,
                                        glyphRun.AdvanceWidths,
                                        null, null, null, null, null, null
                                    );

                                    dc?.DrawGlyphRun(Brushes.Black, drawGlyph);
                                }
                                x += cellWidth;
                            }

                            y += maxRowHeight;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                drawingContext?.DrawText(new FormattedText(ex.Message, CultureInfo.InvariantCulture,
                    System.Windows.FlowDirection.LeftToRight, GuiService.typeface, TextEmSize, Brushes.Red,
                    1.0), new Point(0, 0));
            }
            return this;
        }
        public GUI md(Output output)
        {
            try
            {
                if (drawingContext != null)
                {
                    MarkdownRenderer renderer = new MarkdownRenderer();
                    renderer.DrawMarkdown(drawingContext, output.text(), new Point(20, 20), new Point(Right - 20, Bottom - 20));
                }
            }
            catch (Exception ex)
            {
                drawingContext?.DrawText(new FormattedText(ex.Message, CultureInfo.InvariantCulture,
                    System.Windows.FlowDirection.LeftToRight, GuiService.typeface, TextEmSize, Brushes.Red,
                    1.0), new Point(0, 0));
            }
            return this;
        }
        public GUI svg(Output output)
        {
            if (output.Stream.Length == 0)
                return this;

            try
            {
                var settings = new WpfDrawingSettings();
                settings.IncludeRuntime = true;
                settings.TextAsGeometry = false;

                var svgReader = new FileSvgReader(settings);
                output.Stream.Seek(0, SeekOrigin.Begin);
                var drawing = svgReader.Read(output.Stream);

                var fHeight = (1.0 / drawing.Bounds.Height) * Height;

                var mx = new Matrix();
                mx.Translate(-drawing.Bounds.X, -drawing.Bounds.Y);
                mx.Scale(fHeight, fHeight);

                drawing.Transform = new MatrixTransform(mx);
                drawingContext?.DrawDrawing(drawing);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }

            return this;
        }
        public GUI pdf(Output output)
        {
            if (output.Stream.Length == 0)
                return this;

            try
            {
                PdfDocument document = PdfReader.Open(output.Stream);
                foreach(var item in document.Pages[0].ToArray())
                {
                    var i = item.Value;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }

            return this;
        }
        public GUI text()
        {
            return this;
        }
        public GUI list(Output output)
        {
            return this;
        }
        /// <summary>
        /// Dessine un sélecteur numérique
        /// </summary>
        public GUI level(Output content, string unit, float min, float max, float step)
        {
            var _progress = (1.0 / (max - min)) * (step * content.number());

            // Dessiner le fond de la barre de progression
            Rect backgroundRect = new Rect(0, 0, Width, Height);
            drawingContext?.DrawRectangle(System.Windows.Media.Brushes.Gray, null, backgroundRect);

            // Dessiner la barre de progression
            Rect progressRect = new Rect(0, 0, Width * _progress, Height);
            drawingContext?.DrawRectangle(System.Windows.Media.Brushes.Blue, null, progressRect);

            // Dessiner une bordure
            drawingContext?.DrawRectangle(null, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 1), backgroundRect);

            return this;
        }
        /// <summary>
        /// Dessine un sélecteur à 2 états
        /// </summary>
        public GUI state(Output content, string titleA, string stateA, string titleB, string stateB)
        {
            var _isOn = content.text() == stateA;

            double targetPosition = _isOn ? Width - Height : 0;

            // Fond du bouton
            drawingContext?.DrawRoundedRectangle(System.Windows.Media.Brushes.Gray, null, new Rect(0, 0, Width, Height), 10, 10);

            // Curseur glissant
            drawingContext?.DrawEllipse(System.Windows.Media.Brushes.Blue, null, new System.Windows.Point(targetPosition + Height / 2, Height / 2), Height / 2 - 2, Height / 2 - 2);

            return this;
        }
        /// <summary>
        /// Dessine une séparation entre l'élément précédent et suivant
        /// </summary>
        public GUI separator()
        {
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
            // Dessiner un rectangle avec des coins arrondis
            Rect rect = new Rect(Left, Top, Width, Height);
            drawingContext?.DrawRoundedRectangle(Brushes.Black, null, rect, cornerRadius, cornerRadius);

            return this;
        }
        public GUI background()
        {
            // Dessiner un rectangle avec des coins arrondis
            Rect rect = new Rect(Left, Top, Width, Height);
            drawingContext?.DrawRoundedRectangle(Brushes.LightGray, null, rect, 4, 4);

            return this;
        }
        public GUI circle()
        {
            return this;
        }
        public GUI text(byte[] bytes, int encoding = 65001)
        {
            if (bytes.Length == 0)
                return this;
            var en = Encoding.GetEncoding(encoding);
            var text = en.GetString(bytes);

            double x = Left;
            double y = Top;
            if (String.IsNullOrEmpty(text))
                return this;
            var glyphRun = GUI.ConvertTextToGlyphRun(text, ref x, ref y);
            drawingContext?.DrawGlyphRun(Brushes.Black, glyphRun);

            return this;
        }
        public GUI text(string text)
        {
            if (String.IsNullOrEmpty(text))
                return this;

            double x = Left;
            double y = Top;
            if (String.IsNullOrEmpty(text))
                return this;
            var glyphRun = GUI.ConvertTextToGlyphRun(text, ref x, ref y);
            drawingContext?.DrawGlyphRun(Brushes.Black, glyphRun);

            Top = y;

            return this;
        }
        public GUI text(Output in1)
        {
            if (in1.Stream.Length == 0)
                return this;

            in1.Stream.Seek(0, SeekOrigin.Begin);

            using (var reader = new StreamReader(in1.Stream, Encoding.UTF8, true, 1024, true))
            {
                in1.Stream.Position = 0;
                string text = reader.ReadToEnd();
                in1.Stream.Position = 0;

                foreach (var line in text.Split(new char[] { '\n', '\r' }))
                {
                    double x = Left;
                    double y = Top;
                    if (String.IsNullOrEmpty(line))
                        return this;
                    var glyphRun = GUI.ConvertTextToGlyphRun(line, ref x, ref y);
                    drawingContext?.DrawGlyphRun(Brushes.Black, glyphRun);
                }
            }


            return this;
        }
        public GUI text(string text, string syntax)
        {
            if (String.IsNullOrEmpty(text))
                return this;

            double x = Left;
            double y = Top;
            if (String.IsNullOrEmpty(text))
                return this;
            var glyphRun = GUI.ConvertTextToGlyphRun(text, ref x, ref y);
            drawingContext?.DrawGlyphRun(Brushes.Black, glyphRun);

            return this;
        }
        public GUI text(string[] texts)
        {
            if (texts.Length == 0)
                return this;

            foreach (var text in texts)
            {
                double x = Left;
                double y = Top;
                if (String.IsNullOrEmpty(text))
                    return this;
                var glyphRun = GUI.ConvertTextToGlyphRun(text, ref x, ref y);
                drawingContext?.DrawGlyphRun(Brushes.Black, glyphRun);
            }
            return this;
        }
        public GUI text(IronPython.Runtime.PythonList texts)
        {
            if (texts.Count == 0)
                return this;

            foreach (var text in texts)
            {
                double x = Left;
                double y = Top;
                if (String.IsNullOrEmpty(text?.ToString()))
                    return this;
                var glyphRun = GUI.ConvertTextToGlyphRun(text?.ToString() ?? String.Empty, ref x, ref y);
                drawingContext?.DrawGlyphRun(Brushes.Black, glyphRun);
            }
            return this;
        }
        public GUI image(Output data, string format = "auto")
        {
            if (data.size() == 0)
                return this;
            try
            {
                // Créer une instance de BitmapImage
                data.Stream.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = data.Stream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Charge l'image dans la mémoire
                bitmapImage.EndInit();

                drawingContext?.DrawImage(bitmapImage, new Rect(Top, Left, Right, Bottom));
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }

            return this;
        }

        /// <summary>
        /// Caractères de dessin par défaut
        /// </summary>
        #region
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
            var pixelsPerDip = VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;
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
                catch (System.Collections.Generic.KeyNotFoundException)
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
                ((float)pixelsPerDip),
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
            var pixelsPerDip = VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;
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
                ((float)pixelsPerDip),
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
        #endregion
    }
}
