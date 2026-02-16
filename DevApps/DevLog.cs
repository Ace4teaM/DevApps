using DevApps.Extends;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace DevApps
{
    /// <summary>
    /// Implémente les fonctionnalités de lecture du fichier MarkDown
    /// Le contenu est décomposé en block affichable et en variables globales
    /// </summary>
    /// <remarks>Le fichier contient potentiellement des scripts qui initialise des variables globales</remarks>
    public static class DevLog
    {
        internal static double DefaultWidth = 800;
        internal static List<Block>? Current;

        public class Block
        {
            /// <summary>
            /// Bloc vide
            /// </summary>
            public static readonly Block Empty = new Block{ text = String.Empty, code = String.Empty };
            /// <summary>
            /// Texte complet du bloc
            /// </summary>
            required public string text;
            /// <summary>
            /// Si c'est un code, uniquement la partie entre guillemets ```
            /// </summary>
            required public string code;
            /// <summary>
            /// Composant qui a sert à créer le visuel et la variable
            /// </summary>
            public ExtendedComponent? component;
            /// <summary>
            /// Tache de rendu, null par défaut car la fenêtre n'est pas forcément rendue
            /// </summary>
            public Task<DrawingVisual>? renderTask;
            /// <summary>
            /// Crée la tache de rendu, exécuté par le thread appelant (nécessaire pour le WPF)
            /// </summary>
            public Func<Block, Task<DrawingVisual>> makeRender;
            /// <summary>
            /// Tache d'exécution, null par défaut car uniquement à la demande de l'utilisateur
            /// </summary>
            public Task<Stream>? contentTask;
            /// <summary>
            /// Crée la tache d'execution du script
            /// </summary>
            public Func<Block, Task<Stream>> makeContent;
            /// <summary>
            /// Token d'annulation des tâches variableTask et renderTask
            /// </summary>
            public CancellationTokenSource tokenSource = new CancellationTokenSource();
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
                if (startFree != -1 && (parsed || i == lines.Length))
                {
                    var texts = String.Join(Environment.NewLine, lines.Skip(startFree).Take((endFree - startFree) + 1));

                    var freeBlock = new Block
                    {
                        text = texts,
                        code = String.Empty
                    };

                    // crée la task mais n'est pas immédiatement exécutée
                    // elle le sera plus tard par DesignerLogView
                    freeBlock.makeRender = new Func<Block, Task<DrawingVisual>>((block) =>
                    {
                        var render = new GUI.MarkdownRenderer();
                        var visual = new DrawingVisual();
                        using (DrawingContext dc = visual.RenderOpen())
                        {
                            render.DrawMarkdown(dc, block.text, new Point(0, 0), new Point(DefaultWidth, 10000));
                        }

                        return Task.FromResult(visual);
                    });

                    // ajout du bloc
                    blocks.Add(freeBlock);

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
            var start = Regex.Match(line, @"^([#]+)\s");
            if (start.Success)
            {
                int level = start.Groups[1].Length;

                var text = line.Substring(start.Groups[1].Length).Trim();

                block = new Block
                {
                    text = lines[offset],
                    code = text
                };

                // crée la task mais n'est pas immédiatement exécutée
                // elle le sera plus tard par DesignerLogView
                block.makeRender = new Func<Block, Task<DrawingVisual>>(block =>
                {
                    var visual = new DrawingVisual();
                    using (DrawingContext dc = visual.RenderOpen())
                    {
                        //dessine le texte
                        var ft = new FormattedText(
                            block.code,
                            CultureInfo.CurrentUICulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Segoe UI"),
                            Math.Max(10, 26 - 3 * level),
                            Brushes.Black,
                            1.0);

                        ft.MaxTextWidth = DefaultWidth;

                        dc.DrawText(ft, new Point(0, 0));

                        // dessine la marge inférieur
                        var margin = ft.Height + 4;
                        dc.DrawLine(new Pen(Brushes.Gray, 2), new Point(0, margin), new Point(DefaultWidth, margin));
                    }

                    return Task.FromResult(visual);
                });

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
                        text = String.Join(Environment.NewLine, lines.Skip(offset).Take((end - offset) + 1)),
                        code = code.ToString()
                    };

                    // parse le code
                    if (String.IsNullOrEmpty(name) == false)
                    {
                        try
                        {
                            var component = Program.GetExtendedComponent(name);//TryLoadEngine(lang, out eng) // si inconnu le texte est collé dans la variable

                            block.component = component;
                            // ne crée pas la task pour ne pas immédiatement l'exécutée
                            // elle le sera plus tard par DesignerLogView
                            block.makeContent = new Func<Block, Task<Stream>>(async block => await component.TryMakeContent(block.tokenSource.Token, code));
                            block.makeRender = new Func<Block, Task<DrawingVisual>>(async block =>  await block.component!.TryMakeRender(
                                                                        block.tokenSource.Token,
                                                                        block.code,
                                                                        DevLog.DefaultWidth));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Composant introuvable `{name}`. {ex.Message}");
                        }
                    }
                    else
                    {
                        // bloc de code anonyme
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
