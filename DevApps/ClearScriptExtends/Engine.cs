using DevApps.Scripts;
using Microsoft.ClearScript.V8;

namespace DevApps.ClearScriptExtends
{
    /// <summary>
    /// Implémente l'intégration du moteur ClearScript dans le programme WPF
    /// </summary>
    internal static class Engine
    {
        internal static void Initialize(out ScriptEngine engine, out ScriptScope scope)
        {
            engine = new V8ScriptEngineAdapter();
            scope = engine.CreateScope();

            var v8Engine = ((V8ScriptEngineAdapter)engine).engine;

            // initialise le moteur IronPython
            /* v8Engine.Runtime.LoadAssembly(typeof(Scriban.Template).Assembly);

             // redirige la sortie standard vers la console
             v8Engine.Runtime.IO.RedirectToConsole();

             var modules = v8Engine.GetModuleFilenames();

             var paths = v8Engine.GetSearchPaths().ToArray();
            */
            scope.SetVariable("interpreter", DevApps.Scripts.Interpreter.Instance);
            scope.SetVariable("console", new DevApps.Scripts.Terminal());
            scope.SetVariable("requests", new DevApps.Scripts.Requests());
            scope.SetVariable("types", new DevApps.Scripts.NetTypes());
        }
    }
}
