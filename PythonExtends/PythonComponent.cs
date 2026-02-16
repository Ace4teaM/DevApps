using System.Diagnostics;
using System.Windows.Media;
using python;

namespace DevApps.Extends
{
    public sealed class PythonComponent : ExtendedComponent
    {
        public PythonComponent()
        {
            engine = new();
        }

        public override void Dispose()
            => engine.Dispose();

        public override void SetVariable(string name, object value)
            => engine.AddHostObject(name, value);

        public override async Task<object> TryMakeVariable(CancellationToken cancellationToken, object input)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"{script} {args}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            return (process.ExitCode, output, error);

            return result;
        }

        public override async Task<DrawingVisual> TryMakeRender(CancellationToken cancellationToken, object input, double width)
        {
            throw new NotImplementedException();
        }
    }
}
