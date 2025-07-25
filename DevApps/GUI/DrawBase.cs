using System.Windows;
using System.Windows.Controls;

namespace DevApps.GUI
{
    /// <summary>
    /// Représente un objet visuel présentable dans DesignerView
    /// </summary>
    public abstract class DrawBase : FrameworkElement
    {
        /// <summary>
        /// Position X dans le canvas
        /// </summary>
        public double X
        {
            get
            {
                return Canvas.GetLeft(this);
            }
            set
            {
                Canvas.SetLeft(this, value);
            }
        }

        /// <summary>
        /// Position Y dans le canvas
        /// </summary>
        public double Y
        {
            get
            {
                return Canvas.GetTop(this);
            }
            set
            {
                Canvas.SetTop(this, value);
            }
        }

    }
}
