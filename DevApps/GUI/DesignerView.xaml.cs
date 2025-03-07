using GUI;
using IronPython.Runtime.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerView.xaml
    /// </summary>
    public partial class DesignerView : UserControl, INotifyPropertyChanged
    {
        internal string statusText { get; set; }
        public string StatusText { get => statusText; set { statusText = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StatusText")); } }

        internal bool isDragging = false;
        internal bool isResizing = false;
        internal bool isDoubleClick = false;
        internal System.Timers.Timer lastClickTimer;//timer entre 2 click
        internal Point startMousePosition;
        internal DrawElement? selectedElement;
        internal ResizeDirection resizeDirection;

        private List<ConnectorElement> connectorElements = new List<ConnectorElement>();

        public event PropertyChangedEventHandler? PropertyChanged;

        public DesignerView()
        {
            InitializeComponent();
            this.DataContext = this;

            StatusText = "Ready";

            lastClickTimer = new System.Timers.Timer(TimeSpan.FromMilliseconds(400));
            lastClickTimer.AutoReset = false;
        }

        internal enum ResizeDirection { None, Left, Right, Top, Bottom, TopLeft, TopRight, BottomLeft, BottomRight }

        internal void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            selectedElement = Mouse.DirectlyOver as DrawElement;

            if (selectedElement == null) return;

            if(lastClickTimer.Enabled)
            {
                isDoubleClick = true;
                return;
            }
            else
            {
                lastClickTimer.Start();
            }

            startMousePosition = e.GetPosition(MyCanvas);
            resizeDirection = GetResizeDirection(startMousePosition);

            if (resizeDirection != DesignerView.ResizeDirection.None)
            {
                isResizing = true;
            }
            else
            {
                isDragging = true;
            }

            selectedElement?.CaptureMouse();
        }

        internal void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            //if (selectedElement == null) return;

            var overElement = Mouse.DirectlyOver as DrawElement;
            var text = overElement != null ? overElement.Name : "Ready";
            if (text != StatusText)
            {
                StatusText = text;

                // supprime les connecteurs
                foreach (var c in connectorElements)
                    MyCanvas.Children.Remove(c);
                connectorElements.Clear();

                if (overElement != null)
                {
                    // ajoute les nouveaux connecteurs
                    Program.DevObject.mutexCheckObjectList.WaitOne();
                    Program.DevObject.References.TryGetValue(overElement.Name, out var reference);
                    if (reference != null)
                    {
                        foreach (var pointer in reference.GetPointers())
                        {
                            var connector = new ConnectorElement(
                                overElement,
                                MyCanvas.Children.OfType<DrawElement>().FirstOrDefault(p => p.Name == pointer.Value)
                            );
                            connectorElements.Add(connector);
                            MyCanvas.Children.Add(connector);
                        }
                    }
                    Program.DevObject.mutexCheckObjectList.ReleaseMutex();
                }
            }
            else
            {
                if (overElement != null && (isDragging || isResizing))
                {
                    //actualise les connecteurs existants
                    foreach (var c in connectorElements)
                    {
                        c.UpdatePosition();
                        c.InvalidateVisual();
                    }
                }
            }

            Point currentMousePosition = e.GetPosition(MyCanvas);

            if (isDragging)
            {
                MoveRectangle(currentMousePosition);
            }
            else if (isResizing)
            {
                ResizeRectangle(currentMousePosition);
            }
            else
            {
                UpdateCursor(currentMousePosition);
            }
        }

        internal void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDoubleClick)
                selectedElement?.RunAction(e.GetPosition(MyCanvas));

            isDoubleClick = false;
            isDragging = false;
            isResizing = false;
            selectedElement?.ReleaseMouseCapture();
        }

        internal void MoveRectangle(Point mousePosition)
        {
            double offsetX = mousePosition.X - startMousePosition.X;
            double offsetY = mousePosition.Y - startMousePosition.Y;

            double newLeft = Canvas.GetLeft(selectedElement) + offsetX;
            double newTop = Canvas.GetTop(selectedElement) + offsetY;

            Canvas.SetLeft(selectedElement, newLeft);
            Canvas.SetTop(selectedElement, newTop);

            startMousePosition = mousePosition;
        }

        internal void ResizeRectangle(Point mousePosition)
        {
            double offsetX = mousePosition.X - startMousePosition.X;
            double offsetY = mousePosition.Y - startMousePosition.Y;

            double left = Canvas.GetLeft(selectedElement);
            double top = Canvas.GetTop(selectedElement);
            double width = selectedElement.Width;
            double height = selectedElement.Height;

            switch (resizeDirection)
            {
                case ResizeDirection.Left:
                    width -= offsetX;
                    left += offsetX;
                    break;
                case ResizeDirection.Right:
                    width += offsetX;
                    break;
                case ResizeDirection.Top:
                    height -= offsetY;
                    top += offsetY;
                    break;
                case ResizeDirection.Bottom:
                    height += offsetY;
                    break;
                case ResizeDirection.TopLeft:
                    width -= offsetX;
                    left += offsetX;
                    height -= offsetY;
                    top += offsetY;
                    break;
                case ResizeDirection.TopRight:
                    width += offsetX;
                    height -= offsetY;
                    top += offsetY;
                    break;
                case ResizeDirection.BottomLeft:
                    width -= offsetX;
                    left += offsetX;
                    height += offsetY;
                    break;
                case ResizeDirection.BottomRight:
                    width += offsetX;
                    height += offsetY;
                    break;
            }

            if (width > 10) selectedElement.Width = width;
            if (height > 10) selectedElement.Height = height;

            Canvas.SetLeft(selectedElement, left);
            Canvas.SetTop(selectedElement, top);

            startMousePosition = mousePosition;
        }

        internal void UpdateCursor(Point mousePosition)
        {
            ResizeDirection direction = GetResizeDirection(mousePosition);
            switch (direction)
            {
                case ResizeDirection.Left:
                case ResizeDirection.Right:
                    Cursor = Cursors.SizeWE;
                    break;
                case ResizeDirection.Top:
                case ResizeDirection.Bottom:
                    Cursor = Cursors.SizeNS;
                    break;
                case ResizeDirection.TopLeft:
                case ResizeDirection.BottomRight:
                    Cursor = Cursors.SizeNWSE;
                    break;
                case ResizeDirection.TopRight:
                case ResizeDirection.BottomLeft:
                    Cursor = Cursors.SizeNESW;
                    break;
                default:
                    Cursor = Cursors.Arrow;
                    break;
            }
        }

        internal ResizeDirection GetResizeDirection(Point mousePosition)
        {
            var selectedElement = Mouse.DirectlyOver as DrawElement;
            if (selectedElement == null)
                return ResizeDirection.None;

            double left = Canvas.GetLeft(selectedElement);
            double top = Canvas.GetTop(selectedElement);
            double right = left + selectedElement.Width;
            double bottom = top + selectedElement.Height;
            double margin = 5; // Zone de redimensionnement

            bool nearLeft = mousePosition.X >= left - margin && mousePosition.X <= left + margin;
            bool nearRight = mousePosition.X >= right - margin && mousePosition.X <= right + margin;
            bool nearTop = mousePosition.Y >= top - margin && mousePosition.Y <= top + margin;
            bool nearBottom = mousePosition.Y >= bottom - margin && mousePosition.Y <= bottom + margin;

            if (nearLeft && nearTop) return ResizeDirection.TopLeft;
            if (nearRight && nearTop) return ResizeDirection.TopRight;
            if (nearLeft && nearBottom) return ResizeDirection.BottomLeft;
            if (nearRight && nearBottom) return ResizeDirection.BottomRight;
            if (nearLeft) return ResizeDirection.Left;
            if (nearRight) return ResizeDirection.Right;
            if (nearTop) return ResizeDirection.Top;
            if (nearBottom) return ResizeDirection.Bottom;

            return ResizeDirection.None;
        }
    }
}
