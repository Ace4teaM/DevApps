﻿using System.Windows;
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
        internal Geometry path;
        internal Pen pen = new Pen(Brushes.Black, 2.0);//contour
        internal Brush? brush = null;//remplissage

        public new System.Windows.Size DesiredSize
        {
            get
            {
                return new System.Windows.Size(path.Bounds.Width, path.Bounds.Height);
            }
        }

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
        protected override Size MeasureOverride(Size availableSize)
        {
            return new System.Windows.Size(path.Bounds.Width, path.Bounds.Height);
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
