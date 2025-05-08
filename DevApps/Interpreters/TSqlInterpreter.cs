using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using DevApps.PythonExtends;
using System.IO;
using System.Text;
using static DevApps.Interpreters.TSqlInterpreter.SqlTable;
using static IronPython.Modules._ast;
using static TSqlParser;

namespace DevApps.Interpreters
{
    public class TSqlInterpreter
    {
        internal class SqlTable
        {
            internal class SqlColumn
            {
                public string Name { get; set; }
                public string DataType { get; set; }
                public string? Nullable { get; set; }
                public string? Default { get; set; }
                public string? Identity { get; set; }
                public string? Primary { get; set; }

                public bool IsExactlyEquals(SqlColumn obj)
                {
                    if (obj == null)
                        return false;

                    if (ReferenceEquals(obj, this))
                        return false;

                    return this.Name == obj.Name
                        && this.DataType == obj.DataType
                        && this.Nullable == obj.Nullable
                        && this.Default == obj.Default
                        && this.Identity == obj.Identity
                        && this.Primary == obj.Primary;
                }

                public bool IsSameNameAndType(SqlColumn obj)
                {
                    if (obj == null)
                        return false;

                    if (ReferenceEquals(obj, this))
                        return false;

                    return this.Name == obj.Name
                        && this.DataType == obj.DataType;
                }

                public void Parse(Column_def_table_constraintContext def)
                {
                    if (def.column_definition() != null)
                    {
                        var colDef = def.column_definition();
                        this.Name = colDef.id_().GetText();
                        this.DataType = colDef.data_type().GetText();

                        // Liste des contraintes
                        var elements = colDef.column_definition_element();

                        foreach (var element in elements)
                        {
                            var constraints = element.column_constraint();

                            if (constraints != null)
                            {
                                if (constraints.null_notnull() != null)
                                {
                                    if (constraints.null_notnull().NOT() != null)
                                        this.Nullable = "NOT NULL";
                                    else
                                        this.Nullable = "NULL";
                                }

                                if (constraints.PRIMARY() != null)
                                {
                                    this.Primary = constraints.PRIMARY().GetText();
                                }
                            }

                            if (element.DEFAULT() != null)
                            {
                                this.Default = element.DEFAULT().GetText();
                            }

                            if (element.IDENTITY() != null)
                            {
                                this.Identity = element.IDENTITY().GetText();
                            }
                        }
                    }
                }
            }
            public string Name { get; set; }
            public List<SqlColumn> Columns { get; set; } = new();
        }

        public TSqlInterpreter mergeAll(Output in1, Output in2, Output output)
        {
            StringBuilder script = new StringBuilder();

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

            TSqlGlobalCollector? sqlGlobalCollector1;
            TSqlGlobalCollector? sqlGlobalCollector2;

            {
                var inputStream = new AntlrInputStream(input1);
                var lexer = new TSqlLexer(inputStream);
                var tokens = new CommonTokenStream(lexer);
                var parser = new TSqlParser(tokens);
                var tree = parser.tsql_file();

                var walker = new ParseTreeWalker();
                var collector = new TSqlGlobalCollector(tokens);
                walker.Walk(collector, tree);

                System.Console.WriteLine($"Éléments trouvés : {collector.Members.Count}");
                foreach (var member in collector.Members)
                {
                    System.Console.WriteLine($"\n--- Élément {member.Value.GetType()} ---\n{member.Key}");
                }

                sqlGlobalCollector1 = collector;
            }

            {
                var inputStream = new AntlrInputStream(input2);
                var lexer = new TSqlLexer(inputStream);
                var tokens = new CommonTokenStream(lexer);
                var parser = new TSqlParser(tokens);
                var tree = parser.tsql_file();

                var walker = new ParseTreeWalker();
                var collector = new TSqlGlobalCollector(tokens);
                walker.Walk(collector, tree);

                System.Console.WriteLine($"Éléments trouvés : {collector.Members.Count}");
                foreach (var member in collector.Members)
                {
                    System.Console.WriteLine($"\n--- Élément {member.Value.GetType()} ---\n{member.Key}");
                }

                sqlGlobalCollector2 = collector;
            }

            if (sqlGlobalCollector2 != null && sqlGlobalCollector1 != null)
            {
                foreach (var member in sqlGlobalCollector1.Members)
                {
                    if (sqlGlobalCollector2.Members.ContainsKey(member.Key))
                    {
                        var c1 = sqlGlobalCollector1.Contexts[member.Key];
                        var c2 = sqlGlobalCollector2.Contexts[member.Key];

                        if (c1.GetType() == c2.GetType())
                        {
                            System.Console.WriteLine($"{member.Key} est du même type dans les 2 scripts");

                            if (c1 is Create_tableContext)
                            {
                                var table1 = c1 as Create_tableContext;
                                var table2 = c2 as Create_tableContext;

                                var sqlTable1 = new SqlTable();
                                var sqlTable2 = new SqlTable();

                                sqlTable1.Name = table1.table_name().GetText();
                                sqlTable2.Name = table2.table_name().GetText();

                                foreach (var def in table1.column_def_table_constraints().column_def_table_constraint())
                                {
                                    var c = new SqlColumn();
                                    c.Parse(def);
                                    sqlTable1.Columns.Add(c);
                                }

                                foreach (var def in table2.column_def_table_constraints().column_def_table_constraint())
                                {
                                    var c = new SqlColumn();
                                    c.Parse(def);
                                    sqlTable2.Columns.Add(c);
                                }


                                foreach (var col in sqlTable2.Columns)
                                {
                                    if (sqlTable1.Columns.Count(p => p.Name == col.Name) == 0)
                                    {
                                        script.Append($"ALTER TABLE {sqlTable2.Name} ADD {col.Name} {col.DataType}");

                                        if (col.Nullable == "NOT NULL")
                                            script.Append($" {col.Nullable}");

                                        if (col.Identity != null)
                                            script.Append($" {col.Identity}");

                                        script.AppendLine($";");
                                    }
                                    else
                                    {
                                        var col1 = sqlTable1.Columns.First(p => p.Name == col.Name);

                                        if (col.IsExactlyEquals(col1) == false)
                                        {
                                            script.Append($"ALTER TABLE {sqlTable2.Name} ALTER COLUMN {col.Name} {col.DataType}");

                                            if (col1.Nullable == "NOT NULL")
                                                script.Append($" {col1.Nullable}");

                                            if (col1.Identity != null)
                                                script.Append($" {col1.Identity}");

                                            script.AppendLine($";");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            System.Console.WriteLine($"{member.Key} est d'un type différent dans le 2eme script");
                        }
                    }
                    else
                    {
                        System.Console.WriteLine($"{member.Key} n'existe pas dans le 2eme script");
                        script.AppendLine(member.Value);
                    }
                }
            }

            var bytes = Encoding.UTF8.GetBytes(script.ToString());
            output.Stream.Write(bytes);
            output.Stream.SetLength(bytes.Length);

            return this;
        }

        public TSqlInterpreter selectElement(Output in1, string name, Output output)
        {
            StringBuilder script = new StringBuilder();

            if (in1.Stream.Length == 0)
                return this;

            in1.Stream.Seek(0, SeekOrigin.Begin);
            output.Stream.Seek(0, SeekOrigin.Begin);

            var input1 = Encoding.UTF8.GetString(in1.Stream.ToArray());//encoding a détecter

            TSqlGlobalCollector? sqlGlobalCollector1;

            var inputStream = new AntlrInputStream(input1);
            var lexer = new TSqlLexer(inputStream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new TSqlParser(tokens);
            var tree = parser.tsql_file();

            var walker = new ParseTreeWalker();
            var collector = new TSqlGlobalCollector(tokens);
            walker.Walk(collector, tree);

            System.Console.WriteLine($"Éléments trouvés : {collector.Members.Count}");
            foreach (var member in collector.Members)
            {
                System.Console.WriteLine($"{member.Key}");
                if (member.Key == name)
                {
                    script.AppendLine(member.Value);
                }
            }

            var bytes = Encoding.UTF8.GetBytes(script.ToString());
            output.Stream.Write(bytes);
            output.Stream.SetLength(bytes.Length);

            return this;
        }

        public TSqlInterpreter updateFrom(Output in1, Output in2, Output output)
        {
            StringBuilder script = new StringBuilder();

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

            TSqlGlobalCollector? sqlGlobalCollector1;
            TSqlGlobalCollector? sqlGlobalCollector2;

            {
                var inputStream = new AntlrInputStream(input1);
                var lexer = new TSqlLexer(inputStream);
                var tokens = new CommonTokenStream(lexer);
                var parser = new TSqlParser(tokens);
                var tree = parser.tsql_file();

                var walker = new ParseTreeWalker();
                var collector = new TSqlGlobalCollector(tokens);
                walker.Walk(collector, tree);

                System.Console.WriteLine($"Éléments trouvés : {collector.Members.Count}");
                foreach (var member in collector.Members)
                {
                    System.Console.WriteLine($"\n--- Élément {member.Value.GetType()} ---\n{member.Key}");
                }

                sqlGlobalCollector1 = collector;
            }

            {
                var inputStream = new AntlrInputStream(input2);
                var lexer = new TSqlLexer(inputStream);
                var tokens = new CommonTokenStream(lexer);
                var parser = new TSqlParser(tokens);
                var tree = parser.tsql_file();

                var walker = new ParseTreeWalker();
                var collector = new TSqlGlobalCollector(tokens);
                walker.Walk(collector, tree);

                System.Console.WriteLine($"Éléments trouvés : {collector.Members.Count}");
                foreach (var member in collector.Members)
                {
                    System.Console.WriteLine($"\n--- Élément {member.Value.GetType()} ---\n{member.Key}");
                }

                sqlGlobalCollector2 = collector;
            }

            if (sqlGlobalCollector2 != null && sqlGlobalCollector1 != null)
            {
                foreach (var member in sqlGlobalCollector1.Members)
                {
                    if (sqlGlobalCollector2.Members.ContainsKey(member.Key))
                    {
                        var c1 = sqlGlobalCollector1.Contexts[member.Key];
                        var c2 = sqlGlobalCollector2.Contexts[member.Key];

                        if (c1.GetType() == c2.GetType())
                        {
                            System.Console.WriteLine($"{member.Key} est du même type dans les 2 scripts");

                            if (c1 is Create_tableContext)
                            {
                                var table1 = c1 as Create_tableContext;
                                var table2 = c2 as Create_tableContext;

                                var sqlTable1 = new SqlTable();
                                var sqlTable2 = new SqlTable();

                                sqlTable1.Name = table1.table_name().GetText();
                                sqlTable2.Name = table2.table_name().GetText();

                                foreach (var def in table1.column_def_table_constraints().column_def_table_constraint())
                                {
                                    var c = new SqlColumn();
                                    c.Parse(def);
                                    sqlTable1.Columns.Add(c);
                                }

                                foreach (var def in table2.column_def_table_constraints().column_def_table_constraint())
                                {
                                    var c = new SqlColumn();
                                    c.Parse(def);
                                    sqlTable2.Columns.Add(c);
                                }


                                foreach (var col in sqlTable2.Columns)
                                {
                                    if (sqlTable1.Columns.Count(p => p.Name == col.Name) == 0)
                                    {
                                        script.Append($"ALTER TABLE {sqlTable2.Name} ADD {col.Name} {col.DataType}");

                                        if (col.Nullable == "NOT NULL")
                                            script.Append($" {col.Nullable}");

                                        if (col.Identity != null)
                                            script.Append($" {col.Identity}");

                                        script.AppendLine($";");
                                    }
                                    else
                                    {
                                        var col1 = sqlTable1.Columns.First(p => p.Name == col.Name);

                                        if (col.IsExactlyEquals(col1) == false)
                                        {
                                            script.Append($"ALTER TABLE {sqlTable2.Name} ALTER COLUMN {col.Name} {col.DataType}");

                                            if (col1.Nullable == "NOT NULL")
                                                script.Append($" {col1.Nullable}");

                                            if (col1.Identity != null)
                                                script.Append($" {col1.Identity}");

                                            script.AppendLine($";");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            System.Console.WriteLine($"{member.Key} est d'un type différent dans le 2eme script");
                        }
                    }
                    else
                    {
                        System.Console.WriteLine($"{member.Key} n'existe pas dans le 2eme script");
                        script.AppendLine(member.Value);
                    }
                }
            }

            var bytes = Encoding.UTF8.GetBytes(script.ToString());
            output.Stream.Write(bytes);
            output.Stream.SetLength(bytes.Length);

            return this;
        }
    }

    public class TSqlGlobalCollector : TSqlParserBaseListener
    {
        private readonly ITokenStream _tokens;
        public Dictionary<string, string> Members { get; } = new();
        public Dictionary<string, ParserRuleContext> Contexts { get; } = new();

        public TSqlGlobalCollector(ITokenStream tokens)
        {
            _tokens = tokens;
        }
        public override void EnterCreate_table([NotNull] Create_tableContext context)
        {
            var start = context.Start.StartIndex;
            var stop = context.Stop.StopIndex;
            var interval = new Interval(start, stop);

            var text = _tokens.TokenSource.InputStream.GetText(interval);
            var key = context.table_name().GetText();
            Members.Add(key, text.Trim());
            Contexts.Add(key, context);
        }
    }
}
