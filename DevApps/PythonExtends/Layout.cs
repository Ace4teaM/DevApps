using System.Windows;

namespace DevApps.PythonExtends
{
    /// <summary>
    /// Fournit des méthodes pour gérer une abstraction de layout-ui avec .net
    /// </summary>
    [Obsolete("En phase d'être remplacé par un accès au DrawingContext (.net) depuis le langage Python pour plus de possibilités de dessin")]
    public class Layout
    {
        internal Rect BaseRect = new Rect();
        internal Rect CurrentRect = new Rect();

        public Layout(Rect rect)
        {
            BaseRect = CurrentRect = rect;
        }

        public Layout fill()
        {
            CurrentRect = BaseRect;
            return this;
        }
        public Layout stack(double height)
        {
            CurrentRect.Y = CurrentRect.Y + height;
            CurrentRect.Height = CurrentRect.Height - height;
            return this;
        }
        public Layout top(double height)
        {
            return this;
        }
        public Layout border(double left, double top, double right, double bottom)
        {
            CurrentRect.X = CurrentRect.X + left;
            CurrentRect.Width = CurrentRect.Width - left;

            CurrentRect.Width = CurrentRect.Width - right;

            CurrentRect.Y = CurrentRect.Y + top;
            CurrentRect.Height = CurrentRect.Height - top;

            CurrentRect.Height = CurrentRect.Height - bottom;

            return this;
        }
        public Layout copy()
        {
            return new Layout(CurrentRect);
        }
    }
}
