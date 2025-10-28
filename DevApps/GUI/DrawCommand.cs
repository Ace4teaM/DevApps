using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Program;

namespace DevApps.GUI
{
    internal class DrawCommand : DrawBase
    {
        internal FormattedText? Title;
        internal DevFacet? facet;

        internal static Typeface typeface = new Typeface("Arial");
        internal static Pen connectorPen = new Pen(Brushes.Linen, 3);
        internal System.Windows.Media.Brush? background = null;

        internal static BitmapImage bitmap = new BitmapImage(new Uri("pack://application:,,,/DevCommandIcon.png", UriKind.Absolute));

        internal DrawCommand(string objectName, DevFacet facet, Point pos, DevCommandGroup command)
        {
            this.facet = facet;
            this.Name = objectName;
            this.Width = 100;
            this.Height = 100;
            this.Y = pos.X;
            this.X = pos.Y;

            this.Title = new FormattedText(objectName, CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight, Service.typeface, 10, Brushes.Blue,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (Title != null)
            {
                drawingContext.DrawText(Title, new System.Windows.Point(0, 0));
                drawingContext.DrawImage(bitmap, new Rect(0,0,128, 128));
            }
        }
    }
}
