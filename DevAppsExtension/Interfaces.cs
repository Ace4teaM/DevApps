using System.IO;
using System.Windows.Media;

namespace DevApps.Extends
{
    public abstract class ExtendedComponent : IDisposable
    {
        public abstract void SetVariable(string name, object value);
        public abstract Task<Stream> TryMakeContent(CancellationToken cancellationToken, object input);
        public abstract Task<DrawingVisual> TryMakeRender(CancellationToken cancellationToken, object input, double width);
        public abstract void Dispose();
    }
}
