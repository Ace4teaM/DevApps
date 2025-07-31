using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DevApps.GUI
{
    internal class ConnectorElement : ContentControl
    {
        internal Canvas group = new Canvas();
        internal Path path = new Path();
        internal Path arrow = new Path();
        public DrawElement Source { get; set; }
        public DrawElement? Destination { get; set; }

        public ConnectorElement(DrawElement source, DrawElement? destination)
        {
            Source = source;
            Destination = destination;
            this.Loaded += new RoutedEventHandler(DesignerLink_Loaded);
        }

        void DesignerLink_Loaded(object sender, RoutedEventArgs e)
        {
            path.Fill = null;
            path.Stroke = Brushes.LightGray;
            path.StrokeThickness = 2;

            arrow.Fill = Brushes.LightGray;
            arrow.Stroke = Brushes.LightGray;
            arrow.StrokeThickness = 2;

            UpdatePosition();

            group.Children.Add(path);
            group.Children.Add(arrow);

            base.Content = group;
        }


        public string ParsePoint(Point point)
        {
            return String.Format("{0},{1}", (int)point.X, (int)point.Y);
        }

        public void UpdatePosition()
        {
            path.Data = Geometry.Parse(
                String.Format("M {0} C {1}, {2}, {3}"
                , ParsePoint(SourcePosition)
                , ParsePoint(SourcePosition + SourceVector * 50)
                , ParsePoint(DestinationPosition + DestinationVector * 50)
                , ParsePoint(DestinationPosition)
            ));

            StreamGeometry geometry = new StreamGeometry();

            double size = 6.0;

            switch (DestinationAnchor)
            {
                case AnchorPoint.Left:
                    using (StreamGeometryContext ctx = geometry.Open())
                    {
                        ctx.BeginFigure(new Point(DestinationPosition.X, DestinationPosition.Y), isFilled: true, isClosed: true);
                        ctx.LineTo(new Point(DestinationPosition.X - size, DestinationPosition.Y + size), isStroked: true, isSmoothJoin: false);
                        ctx.LineTo(new Point(DestinationPosition.X - size, DestinationPosition.Y - size), isStroked: true, isSmoothJoin: false);
                    }
                    break;
                case AnchorPoint.Right:
                    using (StreamGeometryContext ctx = geometry.Open())
                    {
                        ctx.BeginFigure(new Point(DestinationPosition.X, DestinationPosition.Y), isFilled: true, isClosed: true);
                        ctx.LineTo(new Point(DestinationPosition.X + size, DestinationPosition.Y + size), isStroked: true, isSmoothJoin: false);
                        ctx.LineTo(new Point(DestinationPosition.X + size, DestinationPosition.Y - size), isStroked: true, isSmoothJoin: false);
                    }
                    break;
                case AnchorPoint.Top:
                    using (StreamGeometryContext ctx = geometry.Open())
                    {
                        ctx.BeginFigure(new Point(DestinationPosition.X, DestinationPosition.Y), isFilled: true, isClosed: true);
                        ctx.LineTo(new Point(DestinationPosition.X + size, DestinationPosition.Y - size), isStroked: true, isSmoothJoin: false);
                        ctx.LineTo(new Point(DestinationPosition.X - size, DestinationPosition.Y - size), isStroked: true, isSmoothJoin: false);
                    }
                    break;
                case AnchorPoint.Bottom:
                    using (StreamGeometryContext ctx = geometry.Open())
                    {
                        ctx.BeginFigure(new Point(DestinationPosition.X, DestinationPosition.Y), isFilled: true, isClosed: true);
                        ctx.LineTo(new Point(DestinationPosition.X + size, DestinationPosition.Y + size), isStroked: true, isSmoothJoin: false);
                        ctx.LineTo(new Point(DestinationPosition.X - size, DestinationPosition.Y + size), isStroked: true, isSmoothJoin: false);
                    }
                    break;
            }

            arrow.Data = geometry;
        }

        public enum AnchorPoint
        {
            Undefined,
            Left,
            Right,
            Bottom,
            Top
        }

        public AnchorPoint SourceAnchor
        {
            get
            {
                if (Source != null && Destination != null)
                {
                    Point centerB = new Point(Destination.X + Destination.Width / 2, Destination.Y + Destination.Height / 2);
                    Point centerA = new Point(Source.X + Source.Width / 2, Source.Y + Source.Height / 2);

                    Vector delta = centerB - centerA;

                    if (Math.Abs(delta.X) > Math.Abs(delta.Y))
                    {
                        return delta.X > 0 ? AnchorPoint.Right : AnchorPoint.Left;
                    }
                    else
                    {
                        return delta.Y > 0 ? AnchorPoint.Bottom : AnchorPoint.Top;
                    }
                }
                return AnchorPoint.Undefined;
            }
        }

        public Point SourcePosition
        {
            get
            {
                switch (SourceAnchor)
                {
                    case AnchorPoint.Left:
                        return new Point(Source.X, Source.Y + (Source.ActualHeight / 2));
                    case AnchorPoint.Right:
                        return new Point(Source.X + Source.ActualWidth, Source.Y + (Source.ActualHeight / 2));
                    case AnchorPoint.Bottom:
                        return new Point(Source.X + (Source.ActualWidth / 2), Source.Y + Source.ActualHeight);
                    case AnchorPoint.Top:
                        return new Point(Source.X + (Source.ActualWidth / 2), Source.Y);
                    default:
                        return new Point();
                }
            }
        }

        public Vector SourceVector
        {
            get
            {
                switch (SourceAnchor)
                {
                    case AnchorPoint.Left:
                        return new Vector(-1.0, 0.0);
                    case AnchorPoint.Right:
                        return new Vector(1.0, 0.0);
                    case AnchorPoint.Bottom:
                        return new Vector(0.0, 1.0);
                    case AnchorPoint.Top:
                        return new Vector(0.0, -1.0);
                    default:
                        return new Vector();
                }
            }
        }

        public AnchorPoint DestinationAnchor
        {
            get
            {
                if (Source != null && Destination != null)
                {
                    Point centerA = new Point(Destination.X + Destination.Width / 2, Destination.Y + Destination.Height / 2);
                    Point centerB = new Point(Source.X + Source.Width / 2, Source.Y + Source.Height / 2);

                    Vector delta = centerB - centerA;

                    if (Math.Abs(delta.X) > Math.Abs(delta.Y))
                    {
                        return delta.X > 0 ? AnchorPoint.Right : AnchorPoint.Left;
                    }
                    else
                    {
                        return delta.Y > 0 ? AnchorPoint.Bottom : AnchorPoint.Top;
                    }
                }
                return AnchorPoint.Undefined;
            }
        }

        public Point DestinationPosition
        {
            get
            {
                if(Destination == null)
                    return new Point();

                switch (DestinationAnchor)
                {
                    case AnchorPoint.Left:
                        return new Point(Destination.X, Destination.Y + (Destination.ActualHeight / 2));
                    case AnchorPoint.Right:
                        return new Point(Destination.X + Destination.ActualWidth, Destination.Y + (Destination.ActualHeight / 2));
                    case AnchorPoint.Bottom:
                        return new Point(Destination.X + (Destination.ActualWidth / 2), Destination.Y + Destination.ActualHeight);
                    case AnchorPoint.Top:
                        return new Point(Destination.X + (Destination.ActualWidth / 2), Destination.Y);
                    default:
                        return new Point();
                }
            }
        }

        public Vector DestinationVector
        {
            get
            {
                switch (DestinationAnchor)
                {
                    case AnchorPoint.Left:
                        return new Vector(-1.0, 0.0);
                    case AnchorPoint.Right:
                        return new Vector(1.0, 0.0);
                    case AnchorPoint.Bottom:
                        return new Vector(0.0, 1.0);
                    case AnchorPoint.Top:
                        return new Vector(0.0, -1.0);
                    default:
                        return new Vector();
                }
            }
        }
    }
}