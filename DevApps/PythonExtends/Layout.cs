using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DevApps.PythonExtends
{
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
