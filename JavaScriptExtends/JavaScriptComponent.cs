using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using System.Globalization;
using System.Text;
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

        public override async Task<Stream> TryMakeContent(CancellationToken cancellationToken, object input)
        {
            using var reg = cancellationToken.Register(() => engine.Interrupt());

            var script = input.ToString();

            var result = await Task.Run(() => engine.Evaluate(script), cancellationToken);

            if (result == null)
                return Stream.Null;


            string text =
                result is ScriptObject
                    ? engine.Script.JSON.stringify(result)
                    : result?.ToString();

            var stream = new MemoryStream(new UTF8Encoding(true).GetBytes(text));
            return stream;
        }

        public override async Task<DrawingVisual> TryMakeRender(CancellationToken cancellationToken, object input, double width)
        {
            throw new NotImplementedException();
        }
    }
}
