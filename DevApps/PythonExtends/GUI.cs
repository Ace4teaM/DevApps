using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DevApps.PythonExtends
{
    /// <summary>
    /// Graphic object interface
    /// </summary>
    public class GUI
    {
        internal DrawingContext? drawingContext;
        public class Shape
        {
            public Shape fill()
            {
                return this;
            }
        }
        public class Layout
        {
            public Layout fill()
            {
                return this;
            }
            public Layout stack()
            {
                return this;
            }
            public Layout top(float height)
            {
                return this;
            }
            public Layout marginLeft(float marging)
            {
                return this;
            }
            public Layout copy()
            {
                return new Layout();
            }
        }

        public Layout layout { get; set; } = new Layout();

        internal void Begin(DrawingContext context)
        {
            drawingContext = context;
        }

        internal void End()
        {
            drawingContext = null;
        }

        public Shape rectangle()
        {
            return new Shape();
        }
        public void text(byte[] bytes, int encoding = 65001)
        {
            var en = Encoding.GetEncoding(encoding);
            var text = en.GetString(bytes);

            drawingContext?.DrawRectangle(Brushes.Red, null, new System.Windows.Rect(0,0,10,10));
        }
        public Shape ellipse()
        {
            return new Shape();
        }
        public Shape image(byte[] data, string format)
        {
            return new Shape();
        }
    }
}
