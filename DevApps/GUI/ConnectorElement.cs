using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DevApps.GUI
{
    internal class ConnectorElement : ContentControl
    {
        Path path = new Path();
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
            path.Stroke = Brushes.Black;
            path.StrokeThickness = 3;

            UpdatePosition();

            base.Content = path;
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

            Console.WriteLine(ParsePoint(SourcePosition) + "," + ParsePoint(DestinationPosition));
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
                if (Source != null || Destination != null)
                {
                    if (Destination.X < Source.X && ((Destination.Y + Destination.ActualHeight < Source.Y) || (Destination.Y < Source.Y + Source.ActualHeight)))//Left
                        return AnchorPoint.Left;
                    if (Destination.X > Source.X && ((Destination.Y + Destination.ActualHeight < Source.Y) || (Destination.Y < Source.Y + Source.ActualHeight)))//Right
                        return AnchorPoint.Right;
                    if (Destination.Y < Source.Y)//Top
                        return AnchorPoint.Top;
                    if (Destination.Y > Source.Y)//Bottom
                        return AnchorPoint.Bottom;
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
                        return new Point(Source.X + (Source.ActualWidth / 2), Source.Y + Source.ActualHeight);
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
                if (Source != null || Destination != null)
                {
                    if (Source.X < Destination.X && ((Source.Y + Source.ActualHeight < Destination.Y) || (Source.Y < Destination.Y + Destination.ActualHeight)))//Left
                        return AnchorPoint.Left;
                    if (Source.X > Destination.X && ((Source.Y + Source.ActualHeight < Destination.Y) || (Source.Y < Destination.Y + Destination.ActualHeight)))//Right
                        return AnchorPoint.Right;
                    if (Source.Y < Destination.Y)//Top
                        return AnchorPoint.Top;
                    if (Source.Y > Destination.Y)//Bottom
                        return AnchorPoint.Bottom;
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
                        return new Point(Destination.X + (Destination.ActualWidth / 2), Destination.Y + Destination.ActualHeight);
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