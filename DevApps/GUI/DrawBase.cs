using System.Windows;
using System.Windows.Controls;

namespace DevApps.GUI
{
    public abstract class DrawBase : FrameworkElement
    {
        public double X
        {
            get
            {
                var canvas = this.Parent as Canvas;
                if (canvas != null)
                    return Canvas.GetLeft(this);
                return 0;
            }
            set
            {
                var canvas = this.Parent as Canvas;
                if (canvas != null)
                    Canvas.SetLeft(this, value);
            }
        }

        public double Y
        {
            get
            {
                var canvas = this.Parent as Canvas;
                if (canvas != null)
                    return Canvas.GetTop(this);
                return 0;
            }
            set
            {
                var canvas = this.Parent as Canvas;
                if (canvas != null)
                    Canvas.SetTop(this, value);
            }
        }

    }
}
