using DevApps.Interpreters;

namespace DevApps.PythonExtends
{
    public class Interpreter
    {
        public static Interpreter Instance = new Interpreter();

        public CInterpreter C = new CInterpreter();
        public CSharpInterpreter CSharp = new CSharpInterpreter();
        public TSqlInterpreter TSql = new TSqlInterpreter();
    }

}
