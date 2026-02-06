using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerLogView.xaml
    /// </summary>
    public partial class DesignerLogView : UserControl
    {
        public class VisualHost : FrameworkElement
        {
            private readonly Func<double, DrawingVisual> _visualFactory;
            private DrawingVisual? _visual;

            public VisualHost(Func<double, DrawingVisual> visualFactory)
            {
                Margin = new Thickness(0, 6, 0, 6);

                _visualFactory = visualFactory
                    ?? throw new ArgumentNullException(nameof(visualFactory));
            }

            protected override int VisualChildrenCount => _visual == null ? 0 : 1;

            protected override Visual GetVisualChild(int index)
            {
                if (_visual == null || index != 0)
                    throw new ArgumentOutOfRangeException();

                return _visual;
            }

            // --- cœur du mécanisme ---
            protected override Size MeasureOverride(Size availableSize)
            {
                double maxWidth = double.IsInfinity(availableSize.Width)
                    ? 10000   // fallback si StackPanel vertical
                    : availableSize.Width;

                RebuildVisual(maxWidth);

                Rect bounds = _visual?.Drawing?.Bounds ?? Rect.Empty;
                return new Size(bounds.Width, bounds.Height);
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                return finalSize;
            }

            private void RebuildVisual(double maxWidth)
            {
                var newVisual = _visualFactory(maxWidth);

                if (_visual != null)
                {
                    RemoveVisualChild(_visual);
                    RemoveLogicalChild(_visual);
                }

                _visual = newVisual;

                AddVisualChild(_visual);
                AddLogicalChild(_visual);
            }

            // Permet de forcer un redraw externe
            public void InvalidateDrawing()
            {
                InvalidateMeasure();
            }
        }

        public DesignerLogView()
        {
            InitializeComponent();

            if (DevLog.Current != null)
            {
                foreach (var item in DevLog.Current)
                {
                    if (item.visual != null)
                        stackPanel.Children.Add(new VisualHost(item.visual));
                }
            }
        }
    }
}
