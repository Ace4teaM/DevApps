using GUI;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using static Program;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerView.xaml
    /// </summary>
    public partial class DesignerView : UserControl, INotifyPropertyChanged
    {
        internal string facette = String.Empty;
        internal bool isDragging = false;
        internal bool isResizing = false;
        internal bool isDoubleClick = false;
        internal System.Timers.Timer lastClickTimer;//timer entre 2 clics
        internal Point startMousePosition;
        internal DrawElement? selectedElement;
        internal ResizeDirection resizeDirection;

        private List<ConnectorElement> connectorElements = new List<ConnectorElement>();

        public event PropertyChangedEventHandler? PropertyChanged;

        public DesignerView(string facette)
        {
            InitializeComponent();
            this.DataContext = this;

            lastClickTimer = new System.Timers.Timer(TimeSpan.FromMilliseconds(400));
            lastClickTimer.AutoReset = false;

            this.facette = facette;
        }

        internal enum ResizeDirection { None, Left, Right, Top, Bottom, TopLeft, TopRight, BottomLeft, BottomRight }

        internal void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            selectedElement = Mouse.DirectlyOver as DrawElement;

            if (selectedElement == null) return;

            // redimensionnement / déplacement
            if (e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Released)
            {
                if (lastClickTimer.Enabled)
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

            // outils
            if (e.RightButton == MouseButtonState.Pressed && e.LeftButton == MouseButtonState.Released)
            {
                ContextMenu menu = new ContextMenu();
                menu.Items.Add(new MenuItem { Header = "Supprimer" });
                menu.Items.Add(new MenuItem { Header = "Propriétés" });
                menu.Items.Add(new MenuItem { Header = "Copier" });
                menu.Items.Add(new MenuItem { Header = "Coller" });
                menu.Items.Add(new MenuItem { Header = "Couper" });
                menu.Items.Add(new MenuItem { Header = "Dupliquer" });
                menu.Items.Add(new MenuItem { Header = "Verrouiller" });
                menu.Items.Add(new MenuItem { Header = "Déverrouiller" });
                menu.Items.Add(new MenuItem { Header = "Envoyer en arrière" });
                menu.Items.Add(new MenuItem { Header = "Envoyer en avant" });
                menu.Items.Add(new MenuItem { Header = "Aligner à gauche" });
                var m = new MenuItem { Header = "Ajouter à la bibliothèque" };
                m.Click += (s, e) =>
                {
                    Program.DevObject.mutexCheckObjectList.WaitOne();
                    Program.DevObject.References.TryGetValue(selectedElement?.Name, out var reference);
                    Program.DevObject.mutexCheckObjectList.ReleaseMutex();

                    if (reference != null)
                    {
                        reference.mutexReadOutput.WaitOne();

                        using TextWriter writer = new StreamWriter(System.IO.Path.Combine(Program.CommonObjDir, selectedElement?.Name));

                        var settings = new JsonSerializerSettings
                        {
                            Formatting = Formatting.Indented
                        };
                        JsonSerializer serializer = JsonSerializer.CreateDefault(settings);

                        serializer.Serialize(writer, new Serializer.DevObject(reference));

                        reference.SaveOutput(selectedElement?.Name, Program.CommonDataDir);

                        reference.mutexReadOutput.ReleaseMutex();
                    }
                };
                menu.Items.Add(m);
                menu.Placement = PlacementMode.Mouse;
                menu.IsOpen = true;

            }
        }

        internal void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            //if (selectedElement == null) return;

            var overElement = Mouse.DirectlyOver as DrawElement;
            var text = overElement != null ? overElement.Name : "Ready";
            if (text != Service.GetStatusText())
            {
                Service.SetStatusText(text);

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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Service.IsInitialized)
            {
                foreach (var obj in DevFacet.References[this.facette].Objects.devObjects)
                {
                    var o = DevObject.References.FirstOrDefault(p=>p.Value == obj);
                    Service.AddShape(o.Key, o.Value.Description, o.Value.GetZone());
                }
            }
        }
    }
}
