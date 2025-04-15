using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DevApps.GUI
{
    public class DrawGeometry : DrawBase
    {
        public DrawGeometry(Geometry path)
        {
            this.path = path;
        }
        protected Geometry path;
        protected Pen pen = new Pen(Brushes.Black, 2.0);//contour
        protected Brush? brush = null;//remplissage

        public bool SetPath(string data)
        {
            try
            {
                path = Geometry.Parse(data);
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
            if (path != null)
            {
                drawingContext.DrawGeometry(brush, pen, path);
            }
        }
    }
}
