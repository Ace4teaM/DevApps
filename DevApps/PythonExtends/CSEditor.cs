using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DevApps.PythonExtends
{
    /// <summary>
    /// C# Script Editor
    /// </summary>
    public class CSEditor
    {
        public CSEditor(string code)
        {
            code = @"
            class TestClass
            {
                void TestMethod()
                {
                    int i;
                }
            }";

            var tree = CSharpSyntaxTree.ParseText(code);
            var diagnostics = tree.GetDiagnostics();

            if (diagnostics.Any())
            {
                // unsuccessful parsing (errors or warnings)
                foreach (var diagnostic in diagnostics)
                {
                    System.Console.WriteLine(diagnostic.ToString());
                }
            }
            else
            {
                // successful parsing
                System.Console.WriteLine("successful parsing");
            }
        }

        public CSEditor inclass(string name)
        {
            return this;
        }
        public CSEditor inproperty(string name)
        {
            return this;
        }
        public CSEditor getset()
        {
            return this;
        }

        public static CSEditor merge(CSEditor a, CSEditor b)
        {
            //Comparer instruction par instruction
            //utiliser ToString pour comparer des blocks de codes en entier avant de descendre dans l'arborescence
            return new CSEditor("");
        }
    }
}
