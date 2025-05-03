using Microsoft.Scripting.Utils;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using static Program;
using static Program.DevFacet;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerView.xaml
    /// </summary>
    public partial class DesignerView : UserControl, INotifyPropertyChanged, IKeyCommand
    {
        public class CommandItem
        {
            public string? Status { get; set; }
            public string? Description { get; set; }
            public string? CommandLine { get; set; }
        }
        
        internal DevFacet facette;
        internal bool isPanning = false;
        internal bool isDragging = false;
        internal bool isResizing = false;
        internal bool isDoubleClick = false;
        internal bool isResizingPanel = false;
        /// <summary>
        /// Maintient la sélection actuelle
        /// </summary>
        internal bool isSelectionMaintained = false;
        /// <summary>
        /// Le curseur est en mode dessin (pointe sur les coordonnées à ajouter à une forme)
        /// </summary>
        internal bool isPointing = false;
        internal System.Timers.Timer lastClickTimer;//timer entre 2 clics
        internal Point startMousePosition;
        internal DrawBase? selectedElement;
        internal DrawBase? lastSelectedElement;
        internal ResizeDirection resizeDirection;

        // bordure utilisé pour encadrer l'objet survolé
        internal System.Windows.Shapes.Rectangle borderOver = new System.Windows.Shapes.Rectangle { StrokeDashArray = [1.0,1.0]  , StrokeThickness = 2, Stroke = System.Windows.Media.Brushes.Gray, Visibility = Visibility.Hidden };

        // Transformation de la vue
        private ScaleTransform _scaleTransform = new ScaleTransform();
        private TranslateTransform _translateTransform = new TranslateTransform();
        private TransformGroup _transformGroup = new TransformGroup();

        public ObservableCollection<CommandItem> CommandsItems { get; set; } = new ObservableCollection<CommandItem>();

        public event PropertyChangedEventHandler? PropertyChanged;

        public static double commandPanelHeight = 0;
        public static double savedCommandPanelHeight = 300;
        public static double commandPanelMaxHeight = 600;
        public double CommandPanelHeight { get { return commandPanelHeight; } set { commandPanelHeight = value; } }
        
        public static bool showCommandsLines = false;
        public bool ShowCommandsLines { get { return showCommandsLines; } set { showCommandsLines = value; } }

        internal DesignerView(DevFacet facette)
        {
            InitializeComponent();
            this.DataContext = this;

            lastClickTimer = new System.Timers.Timer(TimeSpan.FromMilliseconds(400));
            lastClickTimer.AutoReset = false;

            this.facette = facette;

            _transformGroup.Children.Add(_translateTransform);
            MyCanvas.LayoutTransform = _scaleTransform;

            MyCanvas.Children.Add(borderOver);
        }

        internal void DisplayInfos()
        {
            bool selectionChanged = selectedElement != lastSelectedElement;

            var overElement = selectedElement as DrawBase;
            var text = overElement != null ? overElement.Name : "Ready";

            if (overElement == null)
            {
                Service.SetStatusText(String.Empty);

                // Cache le cadre de l'objet
                borderOver.Visibility = Visibility.Hidden;
                // supprime les connecteurs
                foreach (var c in MyCanvas.Children.OfType<ConnectorElement>().ToArray())
                    MyCanvas.Children.Remove(c);
                // supprime les textes
                foreach (var c in MyCanvas.Children.OfType<ConnectorTextElement>().ToArray())
                    MyCanvas.Children.Remove(c);
            }
            else
            {
                // Actualise le cadre de l'objet
                double marge = 5.0;
                borderOver.Fill = Brushes.Transparent;
                borderOver.RenderTransform = _transformGroup;
                borderOver.Visibility = Visibility.Visible;
                Canvas.SetLeft(borderOver, Canvas.GetLeft(overElement) - marge);
                Canvas.SetTop(borderOver, Canvas.GetTop(overElement) - marge);
                borderOver.Width = overElement.Width + marge * 2;
                borderOver.Height = overElement.Height + marge * 2;
                Canvas.SetZIndex(borderOver, int.MinValue);

                // Affiche les connecteurs et nom de l'objet
                if (selectionChanged)
                {
                    Service.SetStatusText(overElement.Name);

                    // supprime les connecteurs
                    foreach (var c in MyCanvas.Children.OfType<ConnectorElement>().ToArray())
                        MyCanvas.Children.Remove(c);

                    // supprime les textes
                    foreach (var c in MyCanvas.Children.OfType<ConnectorTextElement>().ToArray())
                        MyCanvas.Children.Remove(c);

                    if (overElement != null)
                    {
                        // ajoute les nouveaux connecteurs
                        Program.DevObject.mutexCheckObjectList.WaitOne();
                        if (Program.DevObject.References.TryGetValue(overElement.Name, out var reference))
                        {
                            foreach (var pointer in reference.Pointers)
                            {
                                var dst = MyCanvas.Children.OfType<DrawElement>().FirstOrDefault(p => p.Name == pointer.Value.target);
                                if (dst == null)
                                    continue;

                                var connector = new ConnectorElement(
                                    (overElement as DrawElement),
                                    dst
                                );
                                connector.RenderTransform = _transformGroup;
                                MyCanvas.Children.Add(connector);

                                var textBlock = new ConnectorTextElement(
                                    connector,
                                    pointer.Key
                                );
                                textBlock.RenderTransform = _transformGroup;
                                Canvas.SetZIndex(textBlock, 1);
                                Canvas.SetLeft(textBlock, connector.SourcePosition.X - (connector.SourcePosition.X - connector.DestinationPosition.X) / 2.0);
                                Canvas.SetTop(textBlock, connector.SourcePosition.Y - (connector.SourcePosition.Y - connector.DestinationPosition.Y) / 2.0);
                                MyCanvas.Children.Add(textBlock);
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
                        foreach (var c in MyCanvas.Children.OfType<ConnectorElement>().ToArray())
                        {
                            c.UpdatePosition();
                            c.InvalidateVisual();
                        }
                        //actualise les textes existants
                        foreach (var textBlock in MyCanvas.Children.OfType<ConnectorTextElement>().ToArray())
                        {
                            var connector = textBlock.Tag as ConnectorElement;
                            Canvas.SetLeft(textBlock, connector.SourcePosition.X - (connector.SourcePosition.X - connector.DestinationPosition.X) / 2.0);
                            Canvas.SetTop(textBlock, connector.SourcePosition.Y - (connector.SourcePosition.Y - connector.DestinationPosition.Y) / 2.0);
                            textBlock.InvalidateVisual();
                        }
                    }
                }
            }

            lastSelectedElement = selectedElement;
        }

        internal enum ResizeDirection { None, Left, Right, Top, Bottom, TopLeft, TopRight, BottomLeft, BottomRight }

        internal void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isPointing)
            {
                e.Handled = true;
                return;
            }

            if (selectedElement is DrawBase && isDragging || isResizing)
                SaveDisposition(selectedElement);

            if (isDoubleClick && selectedElement is DrawElement)
                (selectedElement as DrawElement)?.RunAction(e.GetPosition(MyCanvas));

            if (isDoubleClick && selectedElement is DrawGeometry)
            {
                var geo = ((selectedElement as DrawGeometry).Tag as DevFacet.Geometry);
                var wnd = new GetText();
                wnd.Value = geo.path;
                wnd.Owner = Window.GetWindow(this);
                wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (wnd.ShowDialog() == true)
                {
                    if ((selectedElement as DrawGeometry).SetPath(wnd.Value))
                    {
                        geo.path = wnd.Value;
                    }
                    else
                        MessageBox.Show("Syntaxe invalide");
                }
            }

            if (isDoubleClick && selectedElement is DrawText)
            {
                var geo = ((selectedElement as DrawText).Tag as DevFacet.Text);
                var wnd = new GetText();
                wnd.Value = geo.text;
                wnd.IsMultiline = true;
                wnd.Owner = Window.GetWindow(this);
                wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (wnd.ShowDialog() == true)
                {
                    if ((selectedElement as DrawText).SetText(wnd.Value))
                    {
                        geo.text = wnd.Value;
                    }
                    else
                        MessageBox.Show("Le texte ne peut pas être vide");
                }
            }

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

        internal void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // détermine le prochain point à l'écran
            if (isPointing)
            {
                if (e.ChangedButton == MouseButton.Right)
                    StopCapturePositions(false);
                else if (e.ChangedButton == MouseButton.Left)
                {
                    if (captureDraw == null)
                    {
                        BeginCapturePositions();
                    }
                    else
                    {
                        if (NextCapturePositions() == false)
                            StopCapturePositions(false);
                    }
                }

                e.Handled = true;
                return;
            }

            // Si CTRL est maintenu on conserve la sélection actuelle
            // Aide au redimensionnement des objets à fond transparent
            if (isSelectionMaintained == false)
            {
                selectedElement = Mouse.DirectlyOver as DrawElement;
            }

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
                /* menu.Items.Add(new MenuItem { Header = "Propriétés" });
                 menu.Items.Add(new MenuItem { Header = "Copier" });
                 menu.Items.Add(new MenuItem { Header = "Coller" });
                 menu.Items.Add(new MenuItem { Header = "Couper" });
                 menu.Items.Add(new MenuItem { Header = "Dupliquer" });
                 menu.Items.Add(new MenuItem { Header = "Verrouiller" });
                 menu.Items.Add(new MenuItem { Header = "Déverrouiller" });
                 menu.Items.Add(new MenuItem { Header = "Envoyer en arrière" });
                 menu.Items.Add(new MenuItem { Header = "Envoyer en avant" });
                 menu.Items.Add(new MenuItem { Header = "Aligner à gauche" });*/

                {
                    var m = new MenuItem { Header = "Construire (Build)" };
                    m.Click += (s, e) =>
                    {
                        Program.DevObject.mutexCheckObjectList.WaitOne();
                        Program.DevObject.References.TryGetValue(selectedElement?.Name, out var reference);
                        Program.DevObject.mutexCheckObjectList.ReleaseMutex();

                        if (reference != null)
                        {
                            Program.DevObject.Build([new KeyValuePair<string, DevObject>(selectedElement.Name, reference)]);
                        }
                    };
                    menu.Items.Add(m);
                }

                menu.Items.Add(new Separator());

                {
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

                            var instance = reference as Program.DevObjectInstance;
                            if (instance == null && reference is Program.DevObjectReference)
                                instance = (reference as Program.DevObjectReference).GetBaseObject();
                            else
                                return;

                            serializer.Serialize(writer, new Serializer.DevObjectInstance(instance));

                            reference.SaveOutput(selectedElement?.Name, Program.CommonSharedPath);

                            reference.mutexReadOutput.ReleaseMutex();
                        }
                    };
                    menu.Items.Add(m);
                }

                menu.Items.Add(new Separator());

                {
                    var m = new MenuItem { Header = "Supprimer" };
                    m.Click += (s, e) =>
                    {
                        if (selectedElement is DrawElement)
                        {
                            var name = selectedElement.Name;
                            MyCanvas.Children.Remove(selectedElement);
                            selectedElement = null;

                            facette.Objects.Remove(name);
                        }

                        if (selectedElement is DrawGeometry)
                        {
                            var facetGeo = (selectedElement as DrawGeometry).Tag as DevFacet.Geometry;

                            MyCanvas.Children.Remove(selectedElement);
                            selectedElement = null;

                            facette.Geometries.Remove(facetGeo);
                        }

                        if (selectedElement is DrawText)
                        {
                            var facetText = (selectedElement as DrawText).Tag as DevFacet.Text;

                            MyCanvas.Children.Remove(selectedElement);
                            selectedElement = null;

                            facette.Texts.Remove(facetText);
                        }
                    };
                    menu.Items.Add(m);
                }

                menu.Items.Add(new Separator());

                if (selectedElement is DrawElement)
                {
                    isSelectionMaintained = true;
                    menu.Closed += Menu_Closed;

                    Program.DevObject.mutexCheckObjectList.WaitOne();
                    if (DevObject.References.TryGetValue(selectedElement.Name, out var src))
                    {
                        foreach (var ptr in src.Pointers)
                        {
                            var m = new MenuItem();
                            m.Header = ptr.Key + " -> " + ptr.Value.target;
                            m.Tag = ptr;

                            // recherche les objets ayant un pointeur sur un élément avec des tags identiques
                            var mExists = new MenuItem { Header = "Existant" };
                            m.Items.Add(mExists);

                            int count = 0;
                            foreach (var dict in DevObject.References)
                            {
                                var key = dict.Key;
                                var obj = dict.Value;
                                if (obj != src)
                                {
                                    if (ptr.Value.tags.Count > 0 && obj.Tags.ContainsAll(ptr.Value.tags))
                                    {
                                        var submenu = new MenuItem { Header = String.IsNullOrEmpty(ptr.Value.target) == false ? obj.Description + " (Remplacera: " + ptr.Value.target + ")" : obj.Description };
                                        submenu.Click += (s, e) =>
                                        {
                                            ptr.Value.target = key;
                                        };
                                        mExists.Items.Add(submenu);
                                        count++;
                                        break;
                                    }
                                }
                            }

                            if (count == 0)
                            {
                                mExists.IsEnabled = false;
                                mExists.Header = mExists.Header.ToString() + " (Aucun)";
                            }

                            m.Items.Add(new Separator());

                            // Nouveaux
                            var mNew = new MenuItem { Header = "Nouveau" };
                            m.Items.Add(mNew);

                            count = 0;
                            var list = new List<Serializer.DevObjectInstance>();
                            if (SharedServices.EnumerateObjects(p => p.Tags.ContainsAll(ptr.Value.tags)/*si compatible avec l'objet*/, Program.CommonSharedPath, ref list) > 0)
                            {
                                foreach (var obj in list)
                                {
                                    var item = new MenuItem();
                                    item.Header = "   " + obj.Description;
                                    item.Tag = obj;
                                    item.Click += MenuItem_AddObject_Click;
                                    mNew.Items.Add(item);
                                    count++;
                                }
                            }

                            if (count == 0)
                            {
                                mNew.IsEnabled = false;
                                mNew.Header = mNew.Header.ToString() + " (Aucun)";
                            }

                            m.IsEnabled = m.Items.Count > 0;
                            menu.Items.Add(m);
                        }
                    }
                    Program.DevObject.mutexCheckObjectList.ReleaseMutex();
                }

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

            DisplayInfos();
        }

        private void Menu_Closed(object sender, RoutedEventArgs e)
        {
            isSelectionMaintained = false;
        }

        internal void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            // détermine le prochain point à l'écran
            if (isPointing)
            {
                e.Handled = true;

                if (capturePath != null)
                {
                    RefreshCapturePositions();
                    return;
                }

                return;
            }

            // Si CTRL est maintenu on conserve la sélection actuelle
            // Aide au redimensionnement des objets à fond transparent
            if (isSelectionMaintained == false)
            {
                selectedElement = Mouse.DirectlyOver as DrawElement;
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

            DisplayInfos();
        }


        public void OnKeyCommand(KeyCommand command)
        {
            if (isPointing && command == KeyCommand.Cancel)
            {
                StopCapturePositions(true);
                return;
            }
        }

        public void OnKeyState(ModifierKeys modifier)
        {
            // Si CTRL est maintenu on conserve la sélection actuelle
            // Aide au redimensionnement des objets à fond transparent
            isSelectionMaintained = (modifier == ModifierKeys.Control);
            InvalidateVisual();
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

        private DrawElement AddElement(string name, DevFacet.ObjectProperties properties)
        {
            var o = DevObject.References.FirstOrDefault(p => p.Key == name);

            var position = properties.GetZone();

            var element = new DrawElement(this.facette);

            element.Title = new FormattedText((o.Value.Description ?? o.Key), CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight, Service.typeface, 10, Brushes.Blue,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            element.SubTitle = new FormattedText(String.Join(' ', o.Value.Tags), CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight, Service.typeface, 8, Brushes.DarkViolet,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            element.Name = o.Key;
            element.Width = position.Width;
            element.Height = position.Height;
            element.RenderTransform = _transformGroup;
            Canvas.SetLeft(element, position.Left);
            Canvas.SetTop(element, position.Top);
            MyCanvas.Children.Add(element);

            return element;
        }

        private DrawGeometry AddGeometry(System.Windows.Media.Geometry geometry, double x, double y)
        {
            var element = new DrawGeometry(geometry);
            element.Tag = geometry;
            element.RenderTransform = _transformGroup;
            Canvas.SetLeft(element, x);
            Canvas.SetTop(element, y);
            MyCanvas.Children.Add(element);
            return element;
        }

        private DrawText AddText(string text, double x, double y)
        {
            var element = new DrawText(text);
            element.Tag = text;
            element.RenderTransform = _transformGroup;
            Canvas.SetLeft(element, x);
            Canvas.SetTop(element, y);
            MyCanvas.Children.Add(element);
            return element;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Service.IsInitialized)
            {
                foreach (var obj in this.facette.Objects)
                {
                    var draw = AddElement(obj.Key, obj.Value);
                    draw.Tag = obj;
                }

                foreach (var obj in this.facette.Geometries)
                {
                    var draw = AddGeometry(System.Windows.Media.Geometry.Parse(obj.path), obj.X, obj.Y);
                    draw.Tag = obj;
                }

                foreach (var obj in this.facette.Texts)
                {
                    var draw = AddText(obj.text, obj.X, obj.Y);
                    draw.Tag = obj;
                }

                CommandsItems.AddRange(this.facette.BuildCommands.Select(p => new CommandItem { Status = "Ready", Description = p.Key, CommandLine = p.Value }));
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
                foreach (var element in MyCanvas.Children.OfType<DrawGeometry>())
                {
                    var src = element.Tag as DevFacet.Geometry;
                    src.X = Canvas.GetLeft(element);
                    src.Y = Canvas.GetTop(element);
                }
                foreach (var element in MyCanvas.Children.OfType<DrawText>())
                {
                    var src = element.Tag as DevFacet.Text;
                    src.X = Canvas.GetLeft(element);
                    src.Y = Canvas.GetTop(element);
                }
            }
        }
        private void SaveDisposition(DrawBase element)
        {
            if (Service.IsInitialized)
            {
                if (element is DrawElement && this.facette.Objects.TryGetValue(element.Name, out var props))
                {
                    props.SetZone(new Rect(Canvas.GetLeft(element), Canvas.GetTop(element), element.Width, element.Height));
                }
                if (element is DrawGeometry)
                {
                    var src = element.Tag as DevFacet.Geometry;
                    src.X = Canvas.GetLeft(element);
                    src.Y = Canvas.GetTop(element);
                }
                if (element is DrawText)
                {
                    var src = element.Tag as DevFacet.Text;
                    src.X = Canvas.GetLeft(element);
                    src.Y = Canvas.GetTop(element);
                }
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
                    Program.DevObject.CompilObjects(objects);
                    Program.DevObject.Init();

                    InvalidateObjects();
                }
            }
        }

        private void Slider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ClickCount == 2)
            {
                if (CommandPanelHeight == 0)
                    CommandPanelHeight = savedCommandPanelHeight;
                else
                {
                    savedCommandPanelHeight = commandPanelHeight;
                    CommandPanelHeight = 0;
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CommandPanelHeight"));
            }

            var element = sender as FrameworkElement;
            element?.CaptureMouse();
            startMousePosition = e.GetPosition(MyCanvas);
            isResizingPanel = true;
        }

        private void Slider_MouseMove(object sender, MouseEventArgs e)
        {
            var element = sender as FrameworkElement;

            if (isResizingPanel == false)
                return;

            Point mousePosition = e.GetPosition(MyCanvas);
            double offsetY = mousePosition.Y - startMousePosition.Y;

            CommandPanelHeight -= offsetY;

            if(CommandPanelHeight < 0)
                CommandPanelHeight = 0;

            if(CommandPanelHeight > commandPanelMaxHeight)
                CommandPanelHeight = commandPanelMaxHeight;

            startMousePosition = mousePosition;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CommandPanelHeight"));
        }

        private void Slider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;

            if (isResizingPanel == false)
                return;

            element?.ReleaseMouseCapture();
            isResizingPanel = false;
        }

        private void ViewCommandsLines_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ShowCommandsLines = !ShowCommandsLines;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ShowCommandsLines"));
        }

        /// <summary>
        /// Gestion du dessin point à point
        /// </summary>
        #region CapturePoints
        internal enum CapturePointMode
        {
            None,
            Text,
            Arrow,
            Rectangle,
            Ellipse,
            Polygon,
            Polyline,
        }

        internal CapturePointMode capturePointMode = CapturePointMode.None;
        internal bool captureCloseable = false;
        internal StringBuilder? capturePath = null;
        internal DrawBase? captureDraw = null;

        private void StartCapturePositions(CapturePointMode mode)
        {
            var position = Mouse.GetPosition(MyCanvas);
            position.X -= _translateTransform.X;
            position.Y -= _translateTransform.Y;

            MyCanvas.Cursor = Cursors.Cross;
            capturePointMode = mode;
            Mouse.Capture(MyCanvas);

            isPointing = true;

            switch (capturePointMode)
            {
                case CapturePointMode.Text:
                    capturePath = new StringBuilder();
                    captureDraw = AddText("Texte", position.X, position.Y);
                    captureCloseable = true;
                    break;
                default:
                    capturePath = null;
                    captureDraw = null;
                    captureCloseable = false;
                    break;
            }
        }

        private void BeginCapturePositions()
        {
            var position = Mouse.GetPosition(MyCanvas);
            position.X -= _translateTransform.X;
            position.Y -= _translateTransform.Y;

            switch (capturePointMode)
            {
                case CapturePointMode.Rectangle:
                case CapturePointMode.Ellipse:
                case CapturePointMode.Arrow:
                case CapturePointMode.Polyline:
                case CapturePointMode.Polygon:
                    capturePath = new StringBuilder("M 0,0");
                    captureDraw = AddGeometry(System.Windows.Media.Geometry.Parse(capturePath.ToString()), position.X, position.Y);
                    captureCloseable = false;
                    break;
            }
        }

        private bool NextCapturePositions()
        {
            var position = Mouse.GetPosition(MyCanvas);
            position.X -= _translateTransform.X;
            position.Y -= _translateTransform.Y;

            switch (capturePointMode)
            {
                case CapturePointMode.None:
                    break;
                case CapturePointMode.Text:
                    {
                        captureCloseable = true;
                    }
                    return false;
                case CapturePointMode.Polyline:
                    {
                        var pos = position - new Point(captureDraw.X, captureDraw.Y);
                        capturePath?.Append(String.Format(" L {0},{1}", (int)pos.X, (int)pos.Y));
                        (captureDraw as DrawGeometry)?.SetPath(capturePath?.ToString());
                        captureCloseable = true;
                    }
                    return true;
                case CapturePointMode.Arrow:
                    {
                        var pos = position - new Point(captureDraw.X, captureDraw.Y);
                        capturePath.Clear();
                        capturePath.Append(String.Format("M 0,0 L {0},{1}", (int)pos.X, (int)pos.Y));

                        double arrowHeadLength = 10;
                        double arrowHeadWidth = 10;
                        Point start = new Point( 0, 0 );
                        Point end = new Point((int)pos.X, (int)pos.Y);
                        Vector direction = end - start;
                        direction.Normalize();

                        // Base de la flèche (début de la tête)
                        Point basePoint = end - direction * arrowHeadLength;

                        // Vecteur perpendiculaire
                        Vector perp = new Vector(-direction.Y, direction.X);

                        // Points de la tête
                        Point left = basePoint + perp * (arrowHeadWidth / 2);
                        Point right = basePoint - perp * (arrowHeadWidth / 2);

                        capturePath.Append(String.Format(" M {0},{1}", (int)left.X, (int)left.Y));
                        capturePath.Append(String.Format(" L {0},{1}", (int)pos.X, (int)pos.Y));
                        capturePath.Append(String.Format(" L {0},{1}", (int)right.X, (int)right.Y));

                        (captureDraw as DrawGeometry)?.SetPath(capturePath.ToString());
                        captureCloseable = true;
                    }
                    return false;
                case CapturePointMode.Ellipse:
                    {
                        var pos = position - new Point(captureDraw.X, captureDraw.Y);
                        capturePath.Clear();
                        capturePath.Append(String.Format("M 0,0 A 1,1 180 1 1 0,{0} M 0,0 A 1,1 180 1 0 0,{0}", (int)pos.X, (int)pos.Y));
                        (captureDraw as DrawGeometry)?.SetPath(capturePath.ToString());
                        captureCloseable = true;
                    }
                    return false;
                case CapturePointMode.Rectangle:
                    {
                        var pos = position - new Point(captureDraw.X, captureDraw.Y);
                        capturePath.Clear();
                        capturePath.Append(String.Format("M 0,0 H {0} V {1} H 0 Z", (int)pos.X, (int)pos.Y));
                        (captureDraw as DrawGeometry)?.SetPath(capturePath.ToString());
                        captureCloseable = true;
                    }
                    return false;
            }

            return true;
        }

        private void RefreshCapturePositions()
        {
            // position du curseur
            var position = Mouse.GetPosition(MyCanvas);
            position.X -= _translateTransform.X;
            position.Y -= _translateTransform.Y;

            // position du clic relatif à la position de l'objet
            var pos = position - new Point(captureDraw.X, captureDraw.Y);

            switch (capturePointMode)
            {
                case CapturePointMode.None:
                    break;
                case CapturePointMode.Text:
                    {
                        (captureDraw as DrawText).X = position.X;
                        (captureDraw as DrawText).Y = position.Y;
                    }
                    break;
                case CapturePointMode.Polyline:
                    {
                        (captureDraw as DrawGeometry)?.SetPath(capturePath + String.Format(" L {0},{1}", (int)pos.X, (int)pos.Y));
                    }
                    break;
                case CapturePointMode.Arrow:
                    {
                        (captureDraw as DrawGeometry)?.SetPath(String.Format("M 0,0 L {0},{1}", (int)pos.X, (int)pos.Y));
                    }
                    break;
                case CapturePointMode.Ellipse:
                    {
                        (captureDraw as DrawGeometry)?.SetPath(String.Format("M 0,0 A 1,1 180 1 1 0,{0} M 0,0 A 1,1 180 1 0 0,{0}", (int)pos.X, (int)pos.Y));
                    }
                    break;
                case CapturePointMode.Rectangle:
                    {
                        (captureDraw as DrawGeometry)?.SetPath(String.Format("M 0,0 H {0} V {1} H 0 Z", (int)pos.X, (int)pos.Y));
                    }
                    break;
            }
        }

        private void StopCapturePositions(bool cancel)
        {
            if (cancel == false && captureCloseable == true)
            {
                if (captureDraw is DrawGeometry)
                {
                    var obj = new DevFacet.Geometry(captureDraw.X, captureDraw.Y, capturePath.ToString());
                    captureDraw.Tag = obj;
                    facette.Geometries.Add(obj);
                }
                if (captureDraw is DrawText)
                {
                    var wnd = new GetText();
                    wnd.Value = "Texte";
                    wnd.Owner = Window.GetWindow(this);

                    if (wnd.ShowDialog() == true && String.IsNullOrWhiteSpace(wnd.Value) == false)
                    {
                        var obj = new DevFacet.Text(captureDraw.X, captureDraw.Y, wnd.Value);
                        captureDraw.Tag = obj;
                        (captureDraw as DrawText).SetText(wnd.Value);
                        facette.Texts.Add(obj);
                    }
                    else
                    {
                        MyCanvas.Children.Remove(captureDraw);
                    }
                }
            }
            else
            {
                MyCanvas.Children.Remove(captureDraw);
            }

            captureDraw = null;
            isPointing = false;
            captureCloseable = false;
            capturePath = null;

            MyCanvas.ReleaseMouseCapture();
            MyCanvas.Cursor = Cursors.Arrow;
        }
        #endregion

        private void MenuItem_Arrow_Click(object sender, RoutedEventArgs e)
        {
            StartCapturePositions(CapturePointMode.Arrow);
        }

        private void MenuItem_Ellipse_Click(object sender, RoutedEventArgs e)
        {
            StartCapturePositions(CapturePointMode.Ellipse);
        }

        private void MenuItem_Line_Click(object sender, RoutedEventArgs e)
        {
            StartCapturePositions(CapturePointMode.Polyline);
        }

        private void MenuItem_Rectangle_Click(object sender, RoutedEventArgs e)
        {
            StartCapturePositions(CapturePointMode.Rectangle);
        }

        private void MenuItem_Text_Click(object sender, RoutedEventArgs e)
        {
            StartCapturePositions(CapturePointMode.Text);
        }

        private void MenuItem_Objects_ContextMenuOpening(object sender, RoutedEventArgs e)
        {
            var list = new List<Serializer.DevObjectInstance>();
            if (SharedServices.EnumerateObjects(p => true, Program.CommonSharedPath, ref list) > 0)
            {
                var menuItem = sender as MenuItem;
                if (menuItem != null)
                {
                    menuItem.Items.Clear();
                    foreach (var obj in list)
                    {
                        var item = new MenuItem();
                        item.Header = obj.Description;
                        item.Tag = obj;
                        item.Click += MenuItem_AddObject_Click;
                        menuItem.Items.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// Ajoute l'objet paramètre dans le layout actif
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_AddObject_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var obj = menuItem?.Tag as Serializer.DevObjectInstance;
            var objects = selectedElement;

            if (obj != null)
            {
                // importe l'objet
                var name = "new";
                if (Program.DevObject.References.ContainsKey(name) == true)
                    Program.DevObject.MakeUniqueName(ref name);

                Program.DevObject.mutexCheckObjectList.WaitOne();
                Program.DevObject.References.Add(name, obj.content);
                Program.DevObject.mutexCheckObjectList.ReleaseMutex();

                // importe les données
                try
                {
                    if (String.IsNullOrEmpty(obj.content.InitialDataBase64) == false)
                    {
                        var data = Convert.FromBase64String(obj.content.InitialDataBase64);
                        obj.content.buildStream.Seek(0, SeekOrigin.Begin);
                        obj.content.buildStream.Write(data);
                        obj.content.buildStream.SetLength(data.Length);
                    }
                }
                catch (Exception ex2)
                {
                    Console.WriteLine(ex2.Message);
                }

                // ajoute à la facette
                var pos = Mouse.GetPosition(MyCanvas);
                var props = new DevFacet.ObjectProperties { title = TitlePlacement.TopLeft, background = "#FFFFFFFF", zone = new Rect(pos, new Size(100, 100)) };
                facette.Objects.Add(name, props);
                AddElement(name, props);

                // associe le nouvel objet à la selection
                // sélectionne le pointeur qui correspond aux tags de l'objet
                if(selectedElement is DrawElement)
                {
                    Program.DevObject.mutexCheckObjectList.WaitOne();
                    if (DevObject.References.TryGetValue(selectedElement.Name, out var src))
                    {
                        try
                        {
                            var ptr = obj.content.Pointers.First(pp => src.Tags.ContainsAll(pp.Value.tags));
                            ptr.Value.target = selectedElement.Name;
                        }
                        catch (Exception ex)
                        {
                            System.Console.WriteLine(ex.Message);
                        }
                    }
                    Program.DevObject.mutexCheckObjectList.ReleaseMutex();
                }

                Program.DevObject.CompilObjects([obj.content]);
                Program.DevObject.Init();// initialise les objets qui ne le sont pas encore
                Service.InvalidateFacets();
            }
        }
    }
}
