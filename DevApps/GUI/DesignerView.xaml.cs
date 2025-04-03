using IronPython.Runtime.Types;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using static IronPython.Modules._ast;
using static Program;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerView.xaml
    /// </summary>
    public partial class DesignerView : UserControl, INotifyPropertyChanged
    {
        internal DevFacet facette;
        internal bool isPanning = false;
        internal bool isDragging = false;
        internal bool isResizing = false;
        internal bool isDoubleClick = false;
        internal System.Timers.Timer lastClickTimer;//timer entre 2 clics
        internal Point startMousePosition;
        internal DrawElement? selectedElement;
        internal ResizeDirection resizeDirection;

        // Transformation de la vue
        private ScaleTransform _scaleTransform = new ScaleTransform();
        private TranslateTransform _translateTransform = new TranslateTransform();
        private TransformGroup _transformGroup = new TransformGroup();

        private List<ConnectorElement> connectorElements = new List<ConnectorElement>();

        public event PropertyChangedEventHandler? PropertyChanged;

        internal DesignerView(DevFacet facette)
        {
            InitializeComponent();
            this.DataContext = this;

            lastClickTimer = new System.Timers.Timer(TimeSpan.FromMilliseconds(400));
            lastClickTimer.AutoReset = false;

            this.facette = facette;

            _transformGroup.Children.Add(_translateTransform);
            MyCanvas.LayoutTransform = _scaleTransform;
        }

        internal enum ResizeDirection { None, Left, Right, Top, Bottom, TopLeft, TopRight, BottomLeft, BottomRight }

        internal void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            selectedElement = Mouse.DirectlyOver as DrawElement;

            // redimensionnement / déplacement
            if (selectedElement != null && e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Released && e.MiddleButton == MouseButtonState.Released)
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
            if (selectedElement != null && e.RightButton == MouseButtonState.Pressed && e.LeftButton == MouseButtonState.Released && e.MiddleButton == MouseButtonState.Released)
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

                        using TextWriter writer = new StreamWriter(System.IO.Path.Combine(Program.CommonObjPath, selectedElement?.Name));

                        var settings = new JsonSerializerSettings
                        {
                            Formatting = Formatting.Indented
                        };
                        JsonSerializer serializer = JsonSerializer.CreateDefault(settings);

                        serializer.Serialize(writer, new Serializer.DevObject(reference));

                        reference.SaveOutput(selectedElement?.Name, Program.CommonDataPath);

                        reference.mutexReadOutput.ReleaseMutex();
                    }
                };
                menu.Items.Add(m);
                menu.Placement = PlacementMode.Mouse;
                menu.IsOpen = true;

            }

            // vue
            if (e.MiddleButton == MouseButtonState.Pressed && e.LeftButton == MouseButtonState.Released && e.RightButton == MouseButtonState.Released)
            {
                startMousePosition = e.GetPosition(MyCanvas);
                MyCanvas.CaptureMouse();

                isPanning = true;
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
                            connector.RenderTransform = _transformGroup;
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
            else if(isPanning)
            {
                PanScroll(currentMousePosition);
            }
            else
            {
                UpdateCursor(currentMousePosition);
            }
        }

        internal void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (selectedElement != null && isDragging || isResizing)
                SaveDisposition(selectedElement);

            if (isDoubleClick)
                selectedElement?.RunAction(e.GetPosition(MyCanvas));

            if (isPanning)
            {
                MyCanvas.ReleaseMouseCapture();
            }

            isPanning = false;
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

        internal void PanScroll(Point mousePosition)
        {
            double offsetX = mousePosition.X - startMousePosition.X;
            double offsetY = mousePosition.Y - startMousePosition.Y;

            _translateTransform.X += offsetX;
            _translateTransform.Y += offsetY;

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

            mousePosition.X -= _translateTransform.X;
            mousePosition.Y -= _translateTransform.Y;

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

        private void AddElement(string name, DevFacet.ObjectProperties properties)
        {
            var o = DevObject.References.FirstOrDefault(p => p.Key == name);

            var position = properties.GetZone();

            var element = new DrawElement(this.facette);
            element.Title = new FormattedText(o.Value.Description ?? o.Key, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Service.typeface, 10, Brushes.Blue);
            element.Name = o.Key;
            element.Width = position.Width;
            element.Height = position.Height;
            element.RenderTransform = _transformGroup;
            Canvas.SetLeft(element, position.Left);
            Canvas.SetTop(element, position.Top);
            MyCanvas.Children.Add(element);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Service.IsInitialized)
            {
                foreach (var obj in this.facette.Objects)
                {
                    AddElement(obj.Key, obj.Value);
                }
            }
        }
        private void SaveDisposition()
        {
            if (Service.IsInitialized)
            {
                foreach (var element in MyCanvas.Children.OfType<DrawElement>())
                {
                    if(this.facette.Objects.TryGetValue(element.Name, out var props))
                        props.SetZone(new Rect(Canvas.GetLeft(element), Canvas.GetTop(element), element.Width, element.Height));
                }
            }
        }
        private void SaveDisposition(DrawElement element)
        {
            if (Service.IsInitialized)
            {
                if (this.facette.Objects.TryGetValue(element.Name, out var props))
                    props.SetZone(new Rect(Canvas.GetLeft(element), Canvas.GetTop(element), element.Width, element.Height));
            }
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point mousePosition = e.GetPosition(MyCanvas);
            double scale = e.Delta > 0 ? 1.1 : (1.0 / 1.1);

            _scaleTransform.ScaleX *= scale;
            _scaleTransform.ScaleY *= scale;

            MyCanvas.LayoutTransform = _scaleTransform;
        }

        internal void InvalidateObjects()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Items"));
        }

        private void dataGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                var objects = new List<Program.DevObject>();

                try
                {
                    foreach (string file in files)
                    {
                        var o = Program.DevObject.CreateFromFile(file, out string name);
                        if (o != null)
                        {
                            objects.Add(o);
                            var pos = e.GetPosition(MyCanvas);
                            var prop = new DevFacet.ObjectProperties { zone = new Rect(pos, new Size(100, 100)) };
                            facette.Objects.Add(name, prop);
                            AddElement(name, prop);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                if (objects.Count > 0)
                {
                    Program.DevObject.MakeReferences(objects);
                    Program.DevObject.Init();

                    InvalidateObjects();
                }
            }
        }
    }
}
