using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using DevApps.Scripts;

namespace DevApps.PythonExtends
{
    /// <summary>
    /// Implémente l'intégration du moteur IronPython dans le programme WPF
    /// </summary>
    internal static class Engine
    {
        internal static void Initialize(out ScriptEngine pyEngine, out ScriptScope pyScope)
        {
            pyEngine = Python.CreateEngine();
            pyScope = pyEngine.CreateScope();

            // initialise le moteur IronPython
            pyEngine.Runtime.LoadAssembly(typeof(Scriban.Template).Assembly);

            pyEngine.ImportModule("array");

            // redirige la sortie standard vers la console
            pyEngine.Runtime.IO.RedirectToConsole();

            var modules = pyEngine.GetModuleFilenames();

            var paths = pyEngine.GetSearchPaths().ToArray();


            pyScope.SetVariable("interpreter", Interpreter.Instance);
            pyScope.SetVariable("console", new DevApps.Scripts.Console());
            pyScope.SetVariable("requests", new DevApps.Scripts.Requests());
            pyScope.SetVariable("types", new DevApps.Scripts.NetTypes());

            //pyScope.ImportModule("openai");
            //pyScope.ImportModule("requests");
            pyScope.ImportModule("json");
        }
    }
}
