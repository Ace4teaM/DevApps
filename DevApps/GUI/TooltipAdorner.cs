using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;

namespace DevApps.GUI
{
    /// <summary>
    /// Implémente le visuel d'une zone d'informations pour un objet
    /// </summary>
    public class TooltipAdorner : Adorner
    {
        private readonly VisualCollection _visuals;
        private readonly Border _tooltipBorder;

        public TooltipAdorner(UIElement adornedElement, string text) : base(adornedElement)
        {
            _tooltipBorder = new Border
            {
                Background = Brushes.LightYellow,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5),
                Child = new TextBlock { Text = text, Foreground = Brushes.Black }
            };

            _visuals = new VisualCollection(this) { _tooltipBorder };
        }

        public void SetPosition(Point position)
        {
            _tooltipBorder.Margin = new Thickness(position.X + 10, position.Y + 10, 0, 0);
            InvalidateVisual();
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _tooltipBorder.Measure(constraint);
            return _tooltipBorder.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _tooltipBorder.Arrange(new Rect(finalSize));
            return finalSize;
        }

        protected override int VisualChildrenCount => _visuals.Count;
        protected override Visual GetVisualChild(int index) => _visuals[index];
    }
}
