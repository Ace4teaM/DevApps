using IronPython.Hosting;

namespace DevApps.Scripts.PythonExtends
{
    /// <summary>
    /// Implémente l'intégration du moteur IronPython dans le programme WPF
    /// </summary>
    internal static class Engine
    {
        internal static void Initialize(out ScriptEngine engine, out ScriptScope scope)
        {
            engine = new PythonScriptEngine();
            scope = engine.CreateScope();

            var pyEngine = ((PythonScriptEngine)engine).engine;
            var pyScope = ((PythonScriptScope)scope).scope;

            // initialise le moteur IronPython
            pyEngine.Runtime.LoadAssembly(typeof(Scriban.Template).Assembly);

            pyEngine.ImportModule("array");

            // redirige la sortie standard vers la console
            pyEngine.Runtime.IO.RedirectToConsole();

            var modules = pyEngine.GetModuleFilenames();

            var paths = pyEngine.GetSearchPaths().ToArray();


            scope.SetVariable("interpreter", DevApps.Scripts.Interpreter.Instance);
            scope.SetVariable("console", new DevApps.Scripts.Terminal());
            scope.SetVariable("requests", new DevApps.Scripts.Requests());
            scope.SetVariable("types", new DevApps.Scripts.NetTypes());

            //pyScope.ImportModule("openai");
            //pyScope.ImportModule("requests");
            pyScope.ImportModule("json");
        }
    }
}
