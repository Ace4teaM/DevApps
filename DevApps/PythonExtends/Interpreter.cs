using DevApps.Interpreters;

namespace DevApps.PythonExtends
{
    /// <summary>
    /// Fournit les instances vers les différents interpréteurs de langages
    /// </summary>
    /// <remarks>
    /// Généralement utilisé par les objet de MergeCode pour fusionner des scripts
    /// </remarks>
    public class Interpreter
    {
        public static Interpreter Instance = new Interpreter();

        public CInterpreter C = new CInterpreter();
        public CSharpInterpreter CSharp = new CSharpInterpreter();
        public TSqlInterpreter TSql = new TSqlInterpreter();
        public DartInterpreter Dart = new DartInterpreter();
    }

}
