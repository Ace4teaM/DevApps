using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using DevApps.Scripts;
using System.IO;
using System.Text;

namespace DevApps.Interpreters
{
    public class DartInterpreter
    {
        /// <summary>
        /// Helper pour trouver les membres d'une classe
        /// </summary>
        internal class FindListener : Dart2ParserBaseListener
        {
            public required string path;
            private string? curClass;

            private readonly ITokenStream _tokens;
            public Dictionary<Tuple<string, string>, Tuple<int, int>> Members { get; } = new();

            public FindListener(ITokenStream tokens)
            {
                _tokens = tokens;
            }

            public override void EnterClassDeclaration([NotNull] Dart2Parser.ClassDeclarationContext context)
            {
                base.EnterClassDeclaration(context);

                var start = context.Start.StartIndex;
                var stop = context.Stop.StopIndex;
                var interval = new Interval(start, stop);

                var text = context.typeIdentifier().IDENTIFIER().ToString();// _tokens.TokenSource.InputStream.GetText(interval);

                curClass = text;

                Members.Add(new("class", text), new (start, stop));
            }

            public override void EnterClassMemberDeclaration([NotNull] Dart2Parser.ClassMemberDeclarationContext context)
            {
                base.EnterClassMemberDeclaration(context);

                var start = context.Start.StartIndex;
                var stop = context.Stop.StopIndex;
                var interval = new Interval(start, stop);

                var type = String.Empty;
                var text = _tokens.TokenSource.InputStream.GetText(interval);

                if (context.methodSignature() != null)
                {
                    if (context.methodSignature().getterSignature() != null)
                    {
                        type = "getter";
                        text = context.methodSignature().getterSignature().identifier()?.GetText();
                        //Program.Logger.WriteLine($"Setter : {name}");
                    }
                    else if (context.methodSignature().setterSignature() != null)
                    {
                        type = "setter";
                        text = context.methodSignature().setterSignature().identifier()?.GetText();
                        //Program.Logger.WriteLine($"Setter : {name}");
                    }
                    else if (context.methodSignature().factoryConstructorSignature() != null)
                    {
                        type = "ctor factory";
                        text = context.methodSignature().factoryConstructorSignature().constructorName()?.GetText();
                        //Program.Logger.WriteLine($"Factory constructeur : {name}");
                    }
                    else if (context.methodSignature().constructorSignature() != null)
                    {
                        type = "ctor";
                        text = context.methodSignature().constructorSignature().constructorName()?.GetText();
                        //Program.Logger.WriteLine("Constructeur sans nom trouvé");
                    }
                    else if (context.methodSignature().STATIC_() != null)
                    {
                        type = "var";
                        text = context.methodSignature().STATIC_().GetText();
                        return;
                    }
                    else
                    {
                        type = "method";
                        text = context.methodSignature().functionSignature().identifier()?.GetText();
                        //Program.Logger.WriteLine($"Méthode : {name}");
                    }
                }
                else if (context.declaration() != null)
                {
                    if (context.declaration().getterSignature() != null)
                    {
                        type = "getter";
                        text = context.declaration().getterSignature().identifier()?.GetText();
                        //Program.Logger.WriteLine($"Setter : {name}");
                    }
                    else if (context.declaration().setterSignature() != null)
                    {
                        type = "setter";
                        text = context.declaration().setterSignature().identifier()?.GetText();
                        //Program.Logger.WriteLine($"Setter : {name}");
                    }
                    else if (context.declaration().factoryConstructorSignature() != null)
                    {
                        type = "ctor factory";
                        text = context.declaration().factoryConstructorSignature().constructorName()?.GetText();
                        //Program.Logger.WriteLine($"Factory constructeur : {name}");
                    }
                    else if (context.declaration().constructorSignature() != null)
                    {
                        type = "ctor";
                        text = context.declaration().constructorSignature().constructorName()?.GetText();
                        //Program.Logger.WriteLine("Constructeur sans nom trouvé");
                    }
                    else if (context.declaration().constantConstructorSignature() != null)
                    {
                        type = "ctor const";
                        text = context.declaration().constantConstructorSignature().constructorName()?.GetText();
                        //Program.Logger.WriteLine("Constructeur sans nom trouvé");
                    }
                    else if (context.declaration().redirectingFactoryConstructorSignature() != null)
                    {
                        type = "ctor redirect";
                        text = context.declaration().redirectingFactoryConstructorSignature().constructorName()?.GetText();
                        //Program.Logger.WriteLine("Constructeur sans nom trouvé");
                    }
                    else if (context.declaration().initializedIdentifierList() != null)
                    {
                        type = "var";
                        foreach (var init in context.declaration().initializedIdentifierList().initializedIdentifier())
                        {
                            text = init.identifier()?.GetText();
                            Members.Add(new(type, curClass + "." + text), new(start, stop));
                        }
                        return;
                        //Program.Logger.WriteLine("Constructeur sans nom trouvé");
                    }
                    else if (context.declaration().staticFinalDeclarationList() != null)
                    {
                        type = "var";
                        foreach (var init in context.declaration().staticFinalDeclarationList().staticFinalDeclaration())
                        {
                            text = init.identifier()?.GetText();
                            Members.Add(new(type, curClass + "." + text), new(start, stop));
                        }
                        return;
                    }
                    else
                    {
                        type = "???";
                        text = context.GetText();
                    }
                }

                Members.Add(new(type, curClass+"."+text), new (start, stop));
            }
        }

        /// <summary>
        /// Recherche un element par son type et son nom
        /// </summary>
        /// <param name="elementType"></param>
        /// <param name="elementName"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public Tuple<int,int>? find(string elementType, string elementName, Output content)
        {
            content.Stream.Position = 0;

            var inputStream = new AntlrInputStream(content.Stream);
            var lexer = new Dart2Lexer(inputStream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new Dart2Parser(tokens);
            var tree = parser.compilationUnit();

            var walker = new ParseTreeWalker();
            var collector = new FindListener(tokens) { path = elementName };
            walker.Walk(collector, tree);

            var found = collector.Members.FirstOrDefault(m => m.Key.Item1 == elementType && m.Key.Item2 == elementName);

            if(found.Key != null)
                return found.Value;

            return null;
        }

        /// <summary>
        /// Recherche une région de code
        /// </summary>
        /// <remarks>
        /// En Dart et par convention les régions sont définies par des commentaires: #region XXXXX et #endregion
        /// </remarks>
        public Tuple<int, int>? findregion(string regionName, Output content)
        {
            content.Stream.Position = 0;

            var inputStream = new AntlrInputStream(content.Stream);
            var lexer = new Dart2Lexer(inputStream);

            // Tous les tokens
            var allTokens = lexer.GetAllTokens();

            // Filtrer ceux sur le canal caché
            var comments = allTokens
                .Where(t => t.Channel == Lexer.Hidden)
                .Select(t => t)
                .ToList();

            //foreach (var c in comments)
           //     Program.Logger.WriteLine($"Commentaire ligne {c.Line}:{c.Column} -> {c.Text}");

            var start = comments.First(p => p.Text.Replace("//","").Trim().StartsWith("#region") && p.Text.Trim().EndsWith(regionName));
            var end = comments.FirstOrDefault(p => p.Text.Replace("//", "").Trim().StartsWith("#endregion") && p.Line > start.Line && p.Text.Trim().EndsWith(regionName));
            if(end == null) // prend le "#endregion" suivant
                end = comments.Where(p => p.Text.Replace("//", "").Trim().StartsWith("#endregion") && p.Line > start.Line).OrderBy(p=>p.Line).First();

            return new Tuple<int, int> (start.StartIndex, end.StopIndex);
        }

        public void replace(string elementName, Output content, Output output)
        {
            var path = elementName.Split('.');
        }

        public DartInterpreter mergeAll(Output in1, Output in2, Output output)
        {
            if (in1.Stream.Length == 0 && in2.Stream.Length == 0)
                return this;

            in1.Stream.Seek(0, SeekOrigin.Begin);
            in2.Stream.Seek(0, SeekOrigin.Begin);
            output.Stream.Seek(0, SeekOrigin.Begin);

            if (in1.Stream.Length > 0 && in2.Stream.Length == 0)
            {
                in1.Stream.CopyTo(output.Stream);
                return this;
            }

            if (in1.Stream.Length == 0 && in2.Stream.Length > 0)
            {
                in2.Stream.CopyTo(output.Stream);
                return this;
            }

            string? input1;
            string? input2;

            using (var reader = new StreamReader(in1.Stream, Encoding.UTF8, true, 1024, true))//encoding a détecter
            {
                in1.Stream.Position = 0;
                input1 = reader.ReadToEnd();
                in1.Stream.Position = 0;
            }

            using (var reader = new StreamReader(in2.Stream, Encoding.UTF8, true, 1024, true))//encoding a détecter
            {
                in2.Stream.Position = 0;
                input2 = reader.ReadToEnd();
                in2.Stream.Position = 0;
            }

            StringBuilder outputString = new StringBuilder();

            return this;
        }
    }
}
