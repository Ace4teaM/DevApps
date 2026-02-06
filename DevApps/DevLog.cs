using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using static Program;

namespace DevApps
{
    /// <summary>
    /// Implémente les fonctionnalités de lecture du fichier MarkDown
    /// Le contenu est décomposé en block affichable et en variables globales
    /// </summary>
    /// <remarks>Le fichier contient potentiellement des scripts qui initialise des variables globales</remarks>
    internal static class DevLog
    {
        internal static List<Block>? Current;

        internal class Block
        {
            public static readonly Block Empty = new Block{ text = String.Empty };
            required public string text;
            public object? variable;
            public Func<double,DrawingVisual>? visual;
        }

        internal static List<Block> ParseContent(string md)
        {
            var lines = md.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            var blocks = new List<Block>();

            int i = 0;
            int startFree = -1;
            int endFree = -1;
            while (i < lines.Length)
            {
                bool parsed = false;
                Block block;
                if(ParseNext(lines, ref i, out block) == true)
                {
                    parsed = true;
                }
                else
                {
                    if (startFree == -1)
                    {
                        startFree = endFree = i;
                    }
                    else
                        endFree++;
                    i++;
                }

                // fin du bloc libre en cours
                if (startFree != -1 && (parsed || i == lines.Length-1))
                {
                    var texts = String.Join(Environment.NewLine, lines.Skip(startFree).Take((endFree - startFree) + 1));

                    var visualFunc = new Func<double,DrawingVisual> ((width) =>
                    {
                        var render = new GUI.MarkdownRenderer();
                        var visual = new DrawingVisual();
                        using (DrawingContext dc = visual.RenderOpen())
                        {
                            dc.DrawRectangle(Brushes.LightBlue, new Pen(Brushes.DarkBlue, 2), new Rect(0, 0, 10, 10));
                            render.DrawMarkdown(dc, texts, new Point(0, 0), new Point(width, 10000));
                        }

                        return visual;
                    });

                    // ajout du bloc
                    blocks.Add(new Block { text = texts, variable = null, visual = visualFunc });

                    startFree = endFree = -1;
                }

                // ajout du prochain bloc
                if(parsed)
                    blocks.Add(block);
            }

            return blocks;
        }

        internal static bool ParseNext(string[] lines, ref int i, out Block block)
        {
            if (TryParseCode(lines, ref i, out block))
            {
                return true;
            }
            else if (TryParseTitle(lines, ref i, out block))
            {
                return true;
            }
            return false;
        }

        internal static bool TryParseTitle(string[] lines, ref int offset, out Block block)
        {
            var line = lines[offset].Trim();
            var start = Regex.Match(line, @"^([#]+)");
            if (start.Success)
            {
                int level = start.Groups[1].Length;

                var text = line.Substring(start.Groups[1].Length).Trim();

                var visualFunc = new Func<double, DrawingVisual>((width) =>
                {
                    var visual = new DrawingVisual();
                    using (DrawingContext dc = visual.RenderOpen())
                    {
                        //dessine le texte
                        var ft = new FormattedText(
                            text,
                            CultureInfo.CurrentUICulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Segoe UI"),
                            Math.Max(10, 26 - 3 * level),
                            Brushes.Black,
                            1.0);

                        ft.MaxTextWidth = width;

                        dc.DrawText(ft, new Point(0, 0));

                        // dessine la marge inférieur
                        var margin = ft.Height + 4;
                        dc.DrawLine(new Pen(Brushes.Gray, 2), new Point(0, margin), new Point(width, margin));
                    }

                    return visual;
                });

                block = new Block
                {
                    text = lines[offset],
                    visual = visualFunc
                };

                offset++;
                return true;
            }

            block = Block.Empty;
            return false;
        }

        internal static bool TryParseCode(string[] lines, ref int offset, out Block block)
        {
            var line = lines[offset].Trim();

            // Code ?
            var start = Regex.Match(line, @"^```([A-z]+)?");
            if (start.Success)
            {
                var name = start.Groups[1].Value;

                var end = Array.FindIndex(lines, offset + 1, lines.Length - (offset + 1), p => p.Trim().StartsWith("```"));
                if (end != -1)
                {
                    // obtient le texte du code
                    StringBuilder code = new();
                    for (int j = offset+1; j < end; j++)
                        code.AppendLine(lines[j]);

                    block = new Block { 
                        text = String.Join(Environment.NewLine, lines.Skip(offset).Take((end - offset) + 1))
                    };

                    // parse le code
                    try
                    {
                        var component = Program.GetExtendedComponent(name);//TryLoadEngine(lang, out eng) // si inconnu le texte est collé dans la variable

                        // ajoute les variables existantes
                        foreach (var variable in DevVariable.References)
                            component.SetVariable(variable.Key, variable.Value.Value);

                        // obtient une variable ?
                        if (component.TryMakeVariable(code, out var outputVar))
                        {
                            block.variable = outputVar!;
                        }

                        // obtient un rendu ?
                        int startIndex = offset;

                        var texts = lines.Skip(startIndex + 1).Take(end - (startIndex+1)).ToArray();

                        var visualFunc = new Func<double, DrawingVisual>((width) =>
                        {
                            var visual = new DrawingVisual();
                            using (DrawingContext dc = visual.RenderOpen())
                            {
                                if (component.TryMakeRender(code, width, dc) == false)
                                {
                                    //dessine le texte

                                    double y = 0;
                                    double lineSpacing = 1.2;

                                    foreach (var text in texts)
                                    {
                                        var ft = new FormattedText(
                                            text,
                                            CultureInfo.CurrentUICulture,
                                            FlowDirection.LeftToRight,
                                            new Typeface("Verdana"),
                                            14,
                                            Brushes.Gray,
                                            1.0);

                                        ft.MaxTextWidth = width;

                                        dc.DrawText(ft, new Point(0, y));

                                        y += ft.Height * lineSpacing;
                                    }
                                }
                            }

                            return visual;
                        });

                        block.visual = visualFunc;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur au parsing du contenu `{name}` ({start}:{end}). {ex.Message}");

                        int startIndex = offset;

                        var texts = lines.Skip(startIndex + 1).Take(end - (startIndex + 1)).ToArray();

                        var visualFunc = new Func<double, DrawingVisual>((width) =>
                        {
                            var visual = new DrawingVisual();
                            using (DrawingContext dc = visual.RenderOpen())
                            {
                                //dessine le texte

                                double y = 0;
                                double lineSpacing = 1.2;

                                foreach (var text in texts)
                                {
                                    var ft = new FormattedText(
                                        text,
                                        CultureInfo.CurrentUICulture,
                                        FlowDirection.LeftToRight,
                                        new Typeface("Segoe UI"),
                                        18,
                                        Brushes.Black,
                                        1.0);

                                    ft.MaxTextWidth = width;

                                    dc.DrawText(ft, new Point(0, y));

                                    y += ft.Height * lineSpacing;
                                }
                            }

                            return visual;
                        });

                        block.visual = visualFunc;
                    }

                    // suivant
                    offset = end + 1;
                    return true;
                }
            }

            block = Block.Empty;
            return false;
        }
    }
}
