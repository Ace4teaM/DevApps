using Microsoft.ClearScript.V8;
using System.Windows.Media;

namespace DevApps.Extends
{
    public sealed class JavaScriptComponent : ExtendedComponent
    {
        internal readonly V8ScriptEngine engine;

        public JavaScriptComponent()
        {
            engine = new();
        }

        public override void Dispose()
            => engine.Dispose();

        public override void SetVariable(string name, object value)
            => engine.AddHostObject(name, value);

        public override bool TryMakeVariable(object input, out object? variable)
        {
            var script = input.ToString();
            engine.Execute(script);
            variable = null;
            return false;
        }

        public override bool TryMakeRender(object input, double width, DrawingContext drawing)
        {
            var script = input.ToString();
            return false;
        }
    }
}
