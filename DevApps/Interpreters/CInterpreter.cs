using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using DevApps.PythonExtends;
using System.IO;
using System.Text;

namespace DevApps.Interpreters
{
    public class CInterpreter
    {
        public CInterpreter mergeAll(Output in1, Output in2, Output output)
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

            var input1 = Encoding.UTF8.GetString(in1.Stream.ToArray());//encoding a détecter
            var input2 = Encoding.UTF8.GetString(in2.Stream.ToArray());//encoding a détecter
            StringBuilder outputString = new StringBuilder();

            {
                var inputStream = new AntlrInputStream(input1);
                var lexer = new CLexer(inputStream);
                var tokens = new CommonTokenStream(lexer);
                var parser = new CParser(tokens);
                var tree = parser.compilationUnit();

                var walker = new ParseTreeWalker();
                var collector = new CGlobalElementCollector(tokens);
                walker.Walk(collector, tree);

                System.Console.WriteLine($"Éléments globaux trouvés : {collector.GlobalElements.Count}");
                for (int i = 0; i < collector.GlobalElements.Count; i++)
                {
                    outputString.AppendLine($"\n--- Élément {i + 1} ---\n{collector.GlobalElements[i]}\n");
                    System.Console.WriteLine($"\n--- Élément {i + 1} ---\n{collector.GlobalElements[i]}\n");
                }
            }

            {
                var inputStream = new AntlrInputStream(input2);
                var lexer = new CLexer(inputStream);
                var tokens = new CommonTokenStream(lexer);
                var parser = new CParser(tokens);
                var tree = parser.compilationUnit();

                var walker = new ParseTreeWalker();
                var collector = new CGlobalElementCollector(tokens);
                walker.Walk(collector, tree);

                System.Console.WriteLine($"Éléments globaux trouvés : {collector.GlobalElements.Count}");
                for (int i = 0; i < collector.GlobalElements.Count; i++)
                {
                    outputString.AppendLine($"\n--- Élément {i + 1} ---\n{collector.GlobalElements[i]}\n");
                    System.Console.WriteLine($"\n--- Élément {i + 1} ---\n{collector.GlobalElements[i]}\n");
                }
            }

            var bytes = Encoding.UTF8.GetBytes(outputString.ToString());
            output.Stream.Write(bytes);
            output.Stream.SetLength(bytes.Length);

            return this;
        }
    }
    public class CGlobalElementCollector : CBaseListener
    {
        private readonly ITokenStream _tokens;
        public List<string> GlobalElements { get; } = new();

        public CGlobalElementCollector(ITokenStream tokens)
        {
            _tokens = tokens;
        }

        public override void EnterExternalDeclaration([NotNull] CParser.ExternalDeclarationContext context)
        {
            var start = context.Start.StartIndex;
            var stop = context.Stop.StopIndex;
            var interval = new Interval(start, stop);

            var text = _tokens.TokenSource.InputStream.GetText(interval);
            GlobalElements.Add(text.Trim());
        }
    }

}
