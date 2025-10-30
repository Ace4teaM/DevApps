using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using DevApps.PythonExtends;
using System.IO;
using System.Text;

namespace DevApps.Interpreters
{
    public class CSharpInterpreter
    {
        public CSharpInterpreter mergeAll(Output in1, Output in2, Output output)
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

            {
                var inputStream = new AntlrInputStream(input1);
                var lexer = new CSharpLexer(inputStream);
                var tokens = new CommonTokenStream(lexer);
                var parser = new CSharpParser(tokens);
                var tree = parser.compilation_unit();

                var walker = new ParseTreeWalker();
                var collector = new CSharpGlobalCollector(tokens);
                walker.Walk(collector, tree);

                System.Console.WriteLine($"Éléments trouvés : {collector.Members.Count}");
                for (int i = 0; i < collector.Members.Count; i++)
                {
                    outputString.AppendLine($"\n--- Élément {i + 1} ---\n{collector.Members[i]}\n");
                    System.Console.WriteLine($"\n--- Élément {i + 1} ---\n{collector.Members[i]}\n");
                }
            }

            {
                var inputStream = new AntlrInputStream(input2);
                var lexer = new CSharpLexer(inputStream);
                var tokens = new CommonTokenStream(lexer);
                var parser = new CSharpParser(tokens);
                var tree = parser.compilation_unit();

                var walker = new ParseTreeWalker();
                var collector = new CSharpGlobalCollector(tokens);
                walker.Walk(collector, tree);

                System.Console.WriteLine($"Éléments trouvés : {collector.Members.Count}");
                for (int i = 0; i < collector.Members.Count; i++)
                {
                    outputString.AppendLine($"\n--- Élément {i + 1} ---\n{collector.Members[i]}\n");
                    System.Console.WriteLine($"\n--- Élément {i + 1} ---\n{collector.Members[i]}\n");
                }
            }

            var bytes = Encoding.UTF8.GetBytes(outputString.ToString());
            output.Stream.Write(bytes);
            output.Stream.SetLength(bytes.Length);

            return this;
        }
    }

    public class CSharpGlobalCollector : CSharpParserBaseListener
    {
        private readonly ITokenStream _tokens;
        public List<string> Members { get; } = new();

        public CSharpGlobalCollector(ITokenStream tokens)
        {
            _tokens = tokens;
        }

        public override void EnterClass_definition([NotNull] CSharpParser.Class_definitionContext context)
        {
            var start = context.Start.StartIndex;
            var stop = context.Stop.StopIndex;
            var interval = new Interval(start, stop);

            var text = _tokens.TokenSource.InputStream.GetText(interval);
            Members.Add(text.Trim());
        }

        public override void EnterMethod_declaration([NotNull] CSharpParser.Method_declarationContext context)
        {
            var start = context.Start.StartIndex;
            var stop = context.Stop.StopIndex;
            var interval = new Interval(start, stop);

            var text = _tokens.TokenSource.InputStream.GetText(interval);
            Members.Add(text.Trim());
        }

        public override void EnterProperty_declaration([NotNull] CSharpParser.Property_declarationContext context)
        {
            var start = context.Start.StartIndex;
            var stop = context.Stop.StopIndex;
            var interval = new Interval(start, stop);

            var text = _tokens.TokenSource.InputStream.GetText(interval);
            Members.Add(text.Trim());
        }
    }
}
