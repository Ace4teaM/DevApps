using System.Globalization;
using System.Windows.Media;

namespace DevApps.GUI
{
    public class DrawText : DrawBase
    {
        public DrawText(string text)
        {
            textBlock = new FormattedText(text, CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight, new Typeface("Verdana"), 16, Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }
        protected FormattedText textBlock;

        public bool SetText(string text)
        {
            try
            {
                textBlock = new FormattedText(text, CultureInfo.InvariantCulture,
                    System.Windows.FlowDirection.LeftToRight, new Typeface("Verdana"), 16, Brushes.Black,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                InvalidateVisual();
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (textBlock != null)
            {
                drawingContext.DrawText(textBlock, new System.Windows.Point(0,0));
            }
        }
    }
}
