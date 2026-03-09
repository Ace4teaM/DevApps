using DevApps.Commands;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static IronPython.Modules._ast;
using static Program;
using static Program.DevFacet;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerView.xaml
    /// </summary>
    public partial class DesignerView : UserControl, INotifyPropertyChanged, IKeyCommand, IInvalidableView
    {
        public class CommandItem
        {
            public string? Status { get; set; }
            public string? Description { get; set; }
            public string? CommandLine { get; set; }
        }

        internal string FacetName
        {
            get
            {
                return this.Name;
            }
        }

        internal DevFacet? Facet
        {
            get
            {
                return DevFacet.References.GetValueOrDefault(FacetName);
            }
        }

        /// La vue est en cours de translation
        internal bool isPanning = false;
        /// L'objet est en cours de déplacement
        internal bool isDragging = false;
        /// L'objet est en cours de redimensionnement
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

        public TransformGroup ObjectsTransform { get {  return _transformGroup; } }

        public ObservableCollection<CommandItem> CommandsItems { get; set; } = new ObservableCollection<CommandItem>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChange([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static double commandPanelHeight = 0;
        public static double savedCommandPanelHeight = 300;
        public static double commandPanelMaxHeight = 600;
        public double CommandPanelHeight { get { return commandPanelHeight; } set { commandPanelHeight = value; } }
        
        public static bool showCommandsLines = false;
        public bool ShowCommandsLines { get { return showCommandsLines; } set { showCommandsLines = value; } }

        public bool PrintVisibility
        {
            get => printVisibility; set{ printVisibility = value; OnPropertyChange(); }
        }

        public double PrintX
        {
            get {  return Facet!.PrintLayout.X; }
        }

        public double PrintY
        {
            get { return Facet!.PrintLayout.Y; }
        }

        public double PrintW
        {
            get { return Facet!.PrintLayout.Width; }
        }

        public double PrintH
        {
            get { return Facet!.PrintLayout.Height; }
        }

        /// <summary>
        /// Représente la zone de contenu des objets
        /// </summary>
        public Rect GetObjectsBounding()
        {
            Rect boundingBox = Rect.Empty;

            foreach (UIElement child in MyCanvas.Children.OfType<DrawBase>())
            {
                if (child is FrameworkElement fe)
                {
                    // Obtenir la position du child dans le Canvas
                    double left = Canvas.GetLeft(fe);
                    double top = Canvas.GetTop(fe);
                    double width = fe.ActualWidth;
                    double height = fe.ActualHeight;

                    // Valeurs par défaut si Left/Top ne sont pas définis (NaN)
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;

                    Rect childRect = new Rect(left, top, width, height);

                    boundingBox.Union(childRect); // agrandit pour inclure le nouveau rect
                }
            }

            return boundingBox;
        }

        internal DesignerView(string facetName)
        {
            InitializeComponent();
            this.DataContext = this;
            this.Name = facetName;

            lastClickTimer = new System.Timers.Timer(TimeSpan.FromMilliseconds(400));
            lastClickTimer.AutoReset = false;

            _transformGroup.Children.Add(_translateTransform);
            MyCanvas.LayoutTransform = _scaleTransform;

            MyCanvas.Children.Add(borderOver);
        }

        private TooltipAdorner? currentAdorner = null;

        private void RemoveAdorner()
        {
            if (currentAdorner != null)
            {
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(RootGrid);
                if (adornerLayer != null)
                {
                    adornerLayer.Remove(currentAdorner);
                }

                currentAdorner = null;
            }
        }

        /// <summary>
        /// Convertie la coordonnées relative sur le Canvas en coordonnées locale (avec transformation Zoom/Pan)
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        internal Point PointToCanvasCoord(Point pos)
        {
            Matrix m = ObjectsTransform.Value;
            m.Invert();
            return m.Transform(pos);
        }

        internal void DisplayInfos()
        {
            bool selectionChanged = selectedElement != lastSelectedElement;

            var overElement = selectedElement as DrawBase;
            var text = overElement != null ? overElement.Name : "Ready";

            if (overElement == null)
            {
                // Masque le nom de l'élément survolé
                RemoveAdorner();

                // Masque le nom dans la barre de status
                var pos = PointToCanvasCoord(Mouse.GetPosition(MyCanvas));
                GuiService.SetStatusText(String.Format("X:{0} Y:{1}", (int)pos.X, (int)pos.Y));

                // Masque le cadre de l'objet
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
                    // Affiche le nom de l'élément survolé
                    if (overElement is FrameworkElement fe)
                    {
                        Point pos = Mouse.GetPosition(this);

                        RemoveAdorner();

                        AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(RootGrid);
                        if (adornerLayer != null && String.IsNullOrWhiteSpace(fe.Name) == false)
                        {
                            currentAdorner = new TooltipAdorner(RootGrid, fe.Name);
                            adornerLayer.Add(currentAdorner);
                        }

                        currentAdorner?.SetPosition(pos);
                    }

                    // Affiche le nom dans la barre de status
                    GuiService.SetStatusText(overElement.Name);

                    // supprime les connecteurs
                    foreach (var c in MyCanvas.Children.OfType<ConnectorElement>().ToArray())
                        MyCanvas.Children.Remove(c);

                    // supprime les textes
                    foreach (var c in MyCanvas.Children.OfType<ConnectorTextElement>().ToArray())
                        MyCanvas.Children.Remove(c);

                    if (overElement is DrawElement)
                    {
                        // ajoute les nouveaux connecteurs
                        try
                        {
                            DevObject._checkLock.Wait();

                            if (Program.DevObject.References.TryGetValue(overElement.Name, out var reference))
                            {
                                foreach (var pointer in reference.Pointers)
                                {
                                    var dst = MyCanvas.Children.OfType<DrawElement>().FirstOrDefault(p => p.Name == pointer.Value.target);
                                    if (dst == null)
                                        continue;

                                    var connector = new ConnectorElement(
                                        ((DrawElement)overElement),
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
                        }
                        finally
                        {
                            DevObject._checkLock.Release();
                        }
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
                            var connector = (ConnectorElement)textBlock.Tag;
                            Canvas.SetLeft(textBlock, connector.SourcePosition.X - (connector.SourcePosition.X - connector.DestinationPosition.X) / 2.0);
                            Canvas.SetTop(textBlock, connector.SourcePosition.Y - (connector.SourcePosition.Y - connector.DestinationPosition.Y) / 2.0);
                            textBlock.InvalidateVisual();
                        }
                    }
                    if (currentAdorner != null && isSelectionMaintained == false)
                    {
                        Point pos = Mouse.GetPosition(this);
                        currentAdorner?.SetPosition(pos);
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

            if (selectedElement != null && (isDragging || isResizing))
                SaveDisposition(selectedElement);

            if (isDoubleClick && selectedElement is DrawElement sel3)
                sel3.RunAction(e.GetPosition(MyCanvas));

            if (isDoubleClick && selectedElement is DrawGeometry sel)
            {
                var geo = Facet!.Geometries.First(p=>p.guid == (Guid)sel.Tag);
                var wnd = new GetText();
                wnd.Value = geo.path;
                wnd.Owner = Window.GetWindow(this);
                wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (wnd.ShowDialog() == true)
                {
                    if (sel.SetPath(wnd.Value))
                    {
                        CommandsService.Run(
                            "Edit geometry",
                            () =>
                            {
                                using (DevFacet.Recorder.Rec(FacetName, Facet))
                                    geo.path = wnd.Value;
                            }
                        ).Wait();
                    }
                    else
                        MessageBox.Show(GuiService.EditorWindow, "Syntaxe invalide");
                }
            }

            if (isDoubleClick && selectedElement is DrawText sel2)
            {
                var text = Facet!.Texts.First(p => p.guid == (Guid)sel2.Tag);
                var wnd = new GetText();
                wnd.Value = text.text;
                wnd.IsMultiline = true;
                wnd.Owner = Window.GetWindow(this);
                wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (wnd.ShowDialog() == true)
                {
                    if (sel2.SetText(wnd.Value))
                    {
                        CommandsService.Run(
                            "Edit text",
                            () =>
                            {
                                using (DevFacet.Recorder.Rec(FacetName, Facet))
                                    text.text = wnd.Value;
                            }
                        ).Wait();
                    }
                    else
                        MessageBox.Show(GuiService.EditorWindow, "Le texte ne peut pas être vide");
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
                selectedElement = Mouse.DirectlyOver as DrawBase;
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

                if (selectedElement is DrawElement && resizeDirection != DesignerView.ResizeDirection.None)
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
                if (selectedElement is DrawGeometry)
                {
                    ContextMenu menu = new ContextMenu();

                    var curElement = selectedElement;
                    {
                        var m = new MenuItem { Header = "Retirer" };
                        m.Click += (s, e) =>
                        {
                            CommandsService.Run(
                                "remove geometry",
                                Features.Facets.RemoveGeometry(this.Name, curElement.Tag.ToString())
                            ).Wait();

                            selectedElement = null;
                        };
                        menu.Items.Add(m);
                    }

                    menu.Placement = PlacementMode.Mouse;
                    menu.IsOpen = true;
                }

                if (selectedElement is DrawText)
                {
                    ContextMenu menu = new ContextMenu();

                    var curElement = selectedElement;
                    {
                        var m = new MenuItem { Header = "Retirer" };
                        m.Click += (s, e) =>
                        {
                            CommandsService.Run(
                                "remove text",
                                Features.Facets.RemoveText(this.Name, curElement.Tag.ToString())
                            ).Wait();

                            selectedElement = null;
                        };
                        menu.Items.Add(m);
                    }

                    menu.Placement = PlacementMode.Mouse;
                    menu.IsOpen = true;
                }

                if (selectedElement is DrawCommand)
                {
                    ContextMenu menu = new ContextMenu();

                    var curElement = selectedElement;
                    {
                        var name = curElement?.Name ?? string.Empty;

                        var m = new MenuItem { Header = "Exécuté" };
                        m.Click += (s, e) =>
                        {
                            Program.DevCommandGroup.References.TryGetValue(name, out var reference);

                            if (reference != null)
                            {
                                reference.Execute();
                            }
                        };
                        menu.Items.Add(m);
                    }

                    menu.Items.Add(new Separator());

                    {
                        var name = curElement?.Name ?? string.Empty;

                        var m = new MenuItem { Header = "Retirer" };
                        m.Click += (s, e) =>
                        {
                            CommandsService.Run(
                                "remove command",
                                Features.Facets.RemoveCommand(this.Name, name)
                            ).Wait();

                            selectedElement = null;
                        };
                        menu.Items.Add(m);
                    }

                    menu.Placement = PlacementMode.Mouse;
                    menu.IsOpen = true;
                }

                if (selectedElement is DrawElement)
                {
                    ContextMenu menu = new ContextMenu();

                    var name = selectedElement?.Name ?? string.Empty;

                    {
                        var m = new MenuItem { Header = "Construire (Build)" };
                        m.Click += (s, e) =>
                        {
                            try
                            {
                                DevObject._executeLock.Wait();

                                try
                                {
                                    DevObject._checkLock.Wait();

                                    if (Program.DevObject.TryGet(name, out var reference))
                                    {
                                        Program.DevObject.BuildTree(new KeyValuePair<string, DevObject>(name, reference));
                                    }
                                }
                                finally
                                {
                                    DevObject._checkLock.Release();
                                }
                            }
                            finally
                            {
                                DevObject._executeLock.Release();
                            }
                        };
                        menu.Items.Add(m);
                    }

                    menu.Items.Add(new Separator());

                    {
                        var m = new MenuItem { Header = "Définir comme modèle" };
                        m.Click += (s, e) =>
                        {
                            Features.Objects.SetAsModel(name);
                        };
                        menu.Items.Add(m);
                    }

                    menu.Items.Add(new Separator());

                    {
                        var m = new MenuItem { Header = "Mettre à jour depuis le modèle" };
                        m.Click += (s, e) =>
                        {
                            Features.Objects.UpdateFromModel(name);
                        };
                        menu.Items.Add(m);
                    }

                    menu.Items.Add(new Separator());

                    {
                        var m = new MenuItem { Header = "Dupliquer" };
                        m.Click += (s, e) =>
                        {
                            var newName = Features.Objects.Duplicate(name).Result;
                            if (String.IsNullOrEmpty(newName) == false && selectedElement != null)
                            {
                                Facet.Objects.Add(newName, new DevFacet.ObjectProperties { zone = new Rect(selectedElement.X + 50, selectedElement.Y + 50, selectedElement.Width, selectedElement.Height) });
                                AddElement(newName, Facet.Objects[name]);
                            }
                        };
                        menu.Items.Add(m);
                    }

                    menu.Items.Add(new Separator());

                    {
                        var m = new MenuItem { Header = "Ajouter à la bibliothèque" };
                        m.Click += (s, e) =>
                        {
                            try
                            {
                                DevObject._checkLock.Wait();

                                if (Program.DevObject.TryGet(name, out var reference))
                                {
                                    try
                                    {
                                        reference._readOutput.Wait();

                                        using TextWriter writer = new StreamWriter(System.IO.Path.Combine(Program.CommonObjPath, name));

                                        var settings = new JsonSerializerSettings
                                        {
                                            Formatting = Formatting.Indented
                                        };
                                        JsonSerializer serializer = JsonSerializer.CreateDefault(settings);

                                        var instance = reference as Program.DevObjectInstance;
                                        if (instance == null && reference is Program.DevObjectReference)
                                            instance = ((Program.DevObjectReference)reference).GetBaseObject();

                                        if (instance == null || selectedElement == null)
                                            return;

                                        serializer.Serialize(writer, new Serializer.DevObjectInstance(instance));

                                        reference.SaveOutput(selectedElement?.Name!, Program.CommonSharedPath);
                                    }
                                    finally
                                    {
                                        reference._readOutput.Release();
                                    }

                                }

                            }
                            finally
                            {
                                DevObject._checkLock.Release();
                            }
                        };
                        menu.Items.Add(m);
                    }

                    menu.Items.Add(new Separator());

                    {
                        var m = new MenuItem { Header = "Retirer" };
                        m.Click += (s, e) =>
                        {
                            CommandsService.Run(
                                "remove object",
                                Features.Facets.RemoveObject(this.Name, name)
                            ).Wait();

                            selectedElement = null;
                        };
                        menu.Items.Add(m);
                    }

                    menu.Items.Add(new Separator());

                    if (selectedElement is DrawElement)
                    {
                        isSelectionMaintained = true;
                        menu.Closed += Menu_Closed;

                        try
                        {
                            DevObject._checkLock.Wait();

                            if (DevObject.References.TryGetValue(name, out var src))
                            {
                                // Pour chaque pointeur, énumère les objets compatibles
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
                                        if (obj != src && key != ptr.Value.target)
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

                                // énumère les objets compatibles
                                {
                                    // Nouveaux
                                    var m = new MenuItem { Header = "Nouvel objet compatible" };

                                    int count = 0;
                                    var list = new List<Serializer.DevObjectInstance>();
                                    if (SharedServices.EnumerateObjects(p => p.Pointers.Any(pp => pp.Value.tags.ContainsAll(src.Tags))/*si compatible avec l'objet*/, Program.CommonSharedPath, ref list) > 0)
                                    {
                                        foreach (var obj in list)
                                        {
                                            var item = new MenuItem();
                                            item.Header = "   " + obj.Description;
                                            item.Tag = obj;
                                            item.Click += MenuItem_AddObject_Click;
                                            m.Items.Add(item);
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        m.Header = m.Header.ToString() + " (Aucun)";
                                    }

                                    m.IsEnabled = m.Items.Count > 0;
                                    menu.Items.Add(m);
                                }
                            }
                        }
                        finally
                        {
                            DevObject._checkLock.Release();
                        }
                    }

                    menu.Placement = PlacementMode.Mouse;
                    menu.IsOpen = true;
                }
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
                selectedElement = Mouse.DirectlyOver as DrawBase;
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
            double width = selectedElement!.Width;
            double height = selectedElement!.Height;

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
            if (selectedElement is DrawElement)
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
                        Cursor = Cursors.ScrollAll;
                        break;
                }
            }
            else if(selectedElement != null)
            {
                Cursor = Cursors.ScrollAll;
            }
            else
            {
                Cursor = Cursors.Arrow;
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

        internal DrawElement AddElement(string name, DevFacet.ObjectProperties properties)
        {
            var o = DevObject.References.FirstOrDefault(p => p.Key == name);

            var position = properties.GetZone();

            var element = new DrawElement(o.Key, this.Facet!, position, o.Key, String.Join(' ', o.Value.Tags));
            element.Name = name;
            element.RenderTransform = _transformGroup;
            MyCanvas.Children.Add(element);

            return element;
        }

        internal void RemoveElement(string name)
        {
            var element = MyCanvas.Children.OfType<DrawElement>().FirstOrDefault(p => p.Name == name);
            if (element != null)
                MyCanvas.Children.Remove(element);
        }

        internal DrawElement? GetElement(string name)
        {
            return MyCanvas.Children.OfType<DrawElement>().FirstOrDefault(p => p.Name == name);
        }

        internal DrawGeometry AddGeometry(DevFacet.Geometry geometry)
        {
            var element = new DrawGeometry(System.Windows.Media.Geometry.Parse(geometry.path));
            element.Tag = geometry.guid;// pas de référence directe à text
            element.DataContext = geometry.path;
            element.RenderTransform = _transformGroup;
            Canvas.SetLeft(element, geometry.X);
            Canvas.SetTop(element, geometry.Y);
            MyCanvas.Children.Add(element);
            return element;
        }

        internal void RemoveGeometry(Guid guid)
        {
            var element = MyCanvas.Children.OfType<DrawGeometry>().FirstOrDefault(p=> (Guid)p.Tag == guid);
            if (element != null)
                MyCanvas.Children.Remove(element);
        }

        internal DrawText AddText(DevFacet.Text text)
        {
            var element = new DrawText(text.text);
            element.Tag = text.guid;// pas de référence directe à text
            element.DataContext = text.text;
            element.RenderTransform = _transformGroup;
            Canvas.SetLeft(element, text.X);
            Canvas.SetTop(element, text.Y);
            MyCanvas.Children.Add(element);
            return element;
        }

        internal DrawCommand AddCommand(string name, DevFacet.CommandProperties properties)
        {
            var o = DevCommandGroup.References.FirstOrDefault(p => p.Key == name);

            var position = properties.GetPosition();

            var element = new DrawCommand(o.Key, this.Facet!, position, o.Value);
            element.Name = name;
            element.RenderTransform = _transformGroup;
            MyCanvas.Children.Add(element);

            return element;
        }

        internal void RemoveText(Guid guid)
        {
            var element = MyCanvas.Children.OfType<DrawText>().FirstOrDefault(p => (Guid)p.Tag == guid);
            if (element != null)
                MyCanvas.Children.Remove(element);
        }


        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (GuiService.IsInitialized)
            {
                foreach (var obj in this.Facet!.Objects)
                {
                    try
                    {
                        AddElement(obj.Key, obj.Value);
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.WriteLine($"Failed to add element {obj.Key}: {ex.Message}");
                    }
                }

                foreach (var obj in this.Facet!.Geometries)
                {
                    try
                    {
                        AddGeometry(obj);
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.WriteLine($"Failed to add geometry {obj.guid}: {ex.Message}");
                    }
                }

                foreach (var obj in this.Facet!.Texts)
                {
                    try
                    {
                        AddText(obj);
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.WriteLine($"Failed to add text {obj.guid}: {ex.Message}");
                    }
                }

                foreach (var obj in this.Facet!.Commands)
                {
                    try
                    {
                        AddCommand(obj.Key, obj.Value);
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.WriteLine($"Failed to add command {obj.Key}: {ex.Message}");
                    }
                }

                //
                // calcule le zoom et la position nécessaire pour afficher le dessin en entier
                //

                // Assure que le layout est fait
                MyCanvas.UpdateLayout();

                // calcule la zone de dessin
                var rect = MyCanvas.GetChildrenBoundingBox();

                // calcule le zoom nécessaire pour afficher le dessin en entier
                var zoom = Math.Min((MyCanvas.ActualWidth / rect.Width),(MyCanvas.ActualHeight / rect.Height));

                _scaleTransform.ScaleX = zoom;
                _scaleTransform.ScaleY = zoom;

                _translateTransform.X = -rect.X;
                _translateTransform.Y = -rect.Y;

                //AddPrintZone(rect);
            }
        }

        /// <summary>
        /// invalide le visuel d'un élément
        /// </summary>
        /// <param name="objectName"></param>
        internal void InvalidateElement(string objectName)
        {
            if (GuiService.IsInitialized)
            {
                var element = GetElement(objectName);
                if (element != null && this.Facet!.Objects.TryGetValue(objectName, out var props))
                {
                    Canvas.SetLeft(element, props.zone.Left);
                    Canvas.SetTop(element, props.zone.Top);
                    element.Width = props.zone.Width;
                    element.Height = props.zone.Height;
                }
            }
        }

        private void SaveDisposition(DrawBase element)
        {
            if (GuiService.IsInitialized)
            {
                if (element is DrawElement && this.Facet!.Objects.TryGetValue(element.Name, out var props))
                {
                    CommandsService.Run(
                        "move object",
                        () => {
                            using (DevFacet.Recorder.Rec(this.Name, this.Facet!))
                                props.SetZone(new Rect(Canvas.GetLeft(element), Canvas.GetTop(element), element.Width, element.Height));
                        }
                    ).Wait();
                }
                if (element is DrawCommand && this.Facet!.Commands.TryGetValue(element.Name, out var props2))
                {
                    CommandsService.Run(
                        "move command",
                        () => {
                            using (DevFacet.Recorder.Rec(this.Name, this.Facet!))
                                props2.SetPosition(new Point(Canvas.GetLeft(element), Canvas.GetTop(element)));
                        }
                     ).Wait();
                }
                if (element is DrawGeometry)
                {
                    var src = Facet!.Geometries.First(p=>p.guid == (Guid)element.Tag);
                    CommandsService.Run(
                        "move geometry",
                        (Action)(() => {
                            using (DevFacet.Recorder.Rec(this.Name, this.Facet!))
                            {
                                src.X = Canvas.GetLeft(element);
                                src.Y = Canvas.GetTop(element);
                            }
                        })
                     ).Wait();
                }
                if (element is DrawText)
                {
                    var src = Facet!.Texts.First(p => p.guid == (Guid)element.Tag);
                    CommandsService.Run(
                        "move text",
                        (Action)(() => {
                            using (DevFacet.Recorder.Rec(this.Name, this.Facet!))
                            {
                                src.X = Canvas.GetLeft(element);
                                src.Y = Canvas.GetTop(element);
                            }
                        })
                     ).Wait();
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

        public void InvalidateContent()
        {
            InvalidateObjects();
            InvalidateCommands();
            InvalidateGeometries();
            InvalidateTexts();
        }

        internal void InvalidateObjects()
        {
            if (GuiService.IsInitialized && this.Facet != null)
            {
                var elements = MyCanvas.Children.OfType<DrawElement>().ToArray();
                foreach (var element in elements)
                    MyCanvas.Children.Remove(element);

                foreach (var obj in this.Facet!.Objects)
                {
                    AddElement(obj.Key, obj.Value);
                }
            }
        }

        internal void InvalidateCommands()
        {
            if (GuiService.IsInitialized && this.Facet != null)
            {
                var elements = MyCanvas.Children.OfType<DrawCommand>().ToArray();
                foreach (var element in elements)
                    MyCanvas.Children.Remove(element);

                foreach (var obj in this.Facet!.Commands)
                {
                    AddCommand(obj.Key, obj.Value);
                }
            }
        }

        internal void InvalidateGeometries()
        {
            if (GuiService.IsInitialized && this.Facet != null)
            {
                var elements = MyCanvas.Children.OfType<DrawGeometry>().ToArray();
                foreach (var element in elements)
                    MyCanvas.Children.Remove(element);

                foreach (var obj in this.Facet!.Geometries)
                {
                    AddGeometry(obj);
                }
            }
        }

        internal void InvalidateTexts()
        {
            if (GuiService.IsInitialized && this.Facet != null)
            {
                var elements = MyCanvas.Children.OfType<DrawText>().ToArray();
                foreach (var element in elements)
                    MyCanvas.Children.Remove(element);

                foreach (var obj in this.Facet!.Texts)
                {
                    AddText(obj);
                }
            }
        }

        private void DataGrid_Drop(object sender, DragEventArgs e)
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
                            pos.X -= _translateTransform.X;
                            pos.Y -= _translateTransform.Y;
                            var prop = new DevFacet.ObjectProperties { zone = new Rect(pos, new Size(100, 100)) };
                            Facet!.Objects.Add(name, prop);
                            AddElement(name, prop);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.WriteLine(ex.Message);
                }

                if (objects.Count > 0)
                {

                    try
                    {
                        DevObject._executeLock.Wait();

                        try
                        {
                            DevObject._checkLock.Wait();

                            Program.DevObject.CompilObjects(objects);
                            Program.DevObject.Init();
                        }
                        finally
                        {
                            DevObject._checkLock.Release();
                        }
                    }
                    finally
                    {
                        DevObject._executeLock.Release();
                    }


                    InvalidateObjects();
                }
            }
            else if (e.Data.GetDataPresent(typeof(DesignerWindow.ObjectModel)))
            {
                var item = (DesignerWindow.ObjectModel)e.Data.GetData(typeof(DesignerWindow.ObjectModel));

                var name = item.Key;
                Program.DevObject.MakeUniqueName(ref name, null);

                // Actualise les pointeurs
                foreach (var ptr in item.Value.content.Pointers)
                {
                    ptr.Value.target = String.Empty;
                }

                // Conserve le guid de base
                item.Value.content.baseGuid = item.Value.content.guid;
                item.Value.content.guid = null;

                // Ajoute aux références
                Program.DevObject.References.Add(name, item.Value.content);

                // importe les données
                try
                {
                    if (String.IsNullOrEmpty(item.Value.InitialDataBase64) == false)
                    {
                        var data = Convert.FromBase64String(item.Value.InitialDataBase64);
                        item.Value.content.buildStream.Seek(0, SeekOrigin.Begin);
                        item.Value.content.buildStream.Write(data);
                        item.Value.content.buildStream.SetLength(data.Length);
                    }
                }
                catch (Exception ex2)
                {
                    Program.Logger.WriteLine(ex2.Message);
                }

                // Ajoute à la facette en cours
                var pos = e.GetPosition(MyCanvas);
                pos.X -= _translateTransform.X;
                pos.Y -= _translateTransform.Y;
                var props = new ObjectProperties() { zone = new Rect(pos, new Point(pos.X + 200, pos.Y + 200)) };
                this.Facet!.Objects.Add(name, props);
                AddElement(name, props);

                try
                {
                    DevObject._executeLock.Wait();

                    try
                    {
                        DevObject._checkLock.Wait();

                        Program.DevObject.CompilObjects([item.Value.content]);
                        Program.DevObject.Init();// initialise les objets qui ne le sont pas encore
                    }
                    finally
                    {
                        DevObject._checkLock.Release();
                    }
                }
                finally
                {
                    DevObject._executeLock.Release();
                }

                GuiService.InvalidateFacets();
            }
            else if (e.Data.GetDataPresent(typeof(FileSystemItem)))
            {
                var item = (FileSystemItem)e.Data.GetData(typeof(FileSystemItem));

                var name = Path.GetFileNameWithoutExtension(item.Name);
                Program.DevObject.MakeUniqueName(ref name, null);

                // Crée l'objet
                var obj = new DevObjectFile(Path.Combine(DataDir, name));

                var ext = Path.GetExtension(item.Name);
                if (ext.Length > 1)
                    obj.tags = new HashSet<string>([ext.Substring(1)]);

                // Détermine le dessin de l'objet en fonction des tags
                obj.drawCode = DevObject.DrawCodeFromExt(ext);

                // Crée la référence vers le fichier
                var file = new DevFile(Path.GetRelativePath(Environment.CurrentDirectory, item.FullPath), name);

                obj.Description = file.Filename;

                // Ajoute aux références
                Program.DevFile.References.Add(file.filename, file);
                Program.DevObject.References.Add(name, obj);

                // Ajoute à la facette en cours
                var pos = e.GetPosition(MyCanvas);
                pos.X -= _translateTransform.X;
                pos.Y -= _translateTransform.Y;
                var props = new ObjectProperties() { zone = new Rect(pos, new Point(pos.X + 200, pos.Y + 200)) };
                this.Facet!.Objects.Add(name, props);
                AddElement(name, props);

                // Copie le contenu du fichier
                if (File.Exists(file.Filename) && obj.filename != null)
                {
                    File.Copy(file.Filename, obj.filename, true);
                    obj.LoadContent();
                }

                try
                {
                    DevObject._executeLock.Wait();

                    try
                    {
                        DevObject._checkLock.Wait();

                        Program.DevObject.CompilObjects([obj]);
                        Program.DevObject.Init();// initialise les objets qui ne le sont pas encore
                    }
                    finally
                    {
                        DevObject._checkLock.Release();
                    }
                }
                finally
                {
                    DevObject._executeLock.Release();
                }

                GuiService.InvalidateFacets();
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
                OnPropertyChange(nameof(CommandPanelHeight));
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
            OnPropertyChange(nameof(CommandPanelHeight));
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
            OnPropertyChange(nameof(ShowCommandsLines));
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
            PrintZone,
        }

        internal CapturePointMode capturePointMode = CapturePointMode.None;
        internal bool captureCloseable = false;
        internal StringBuilder? capturePath = null;
        internal object? captureObject = null;
        internal DrawBase? captureDraw = null;
        private bool printVisibility;

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
                    captureObject = new DevFacet.Text(position.X, position.Y, "Texte");
                    captureDraw = AddText((DevFacet.Text)captureObject);
                    captureCloseable = true;
                    break;
                default:
                    capturePath = null;
                    captureObject = null;
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
                case CapturePointMode.PrintZone:
                    capturePath = new StringBuilder("M 0,0");
                    captureObject = new DevFacet.Geometry(position.X, position.Y, capturePath.ToString());
                    captureDraw = AddGeometry((DevFacet.Geometry)captureObject);
                    captureCloseable = false;
                    break;
                case CapturePointMode.Rectangle:
                case CapturePointMode.Ellipse:
                case CapturePointMode.Arrow:
                case CapturePointMode.Polyline:
                case CapturePointMode.Polygon:
                    capturePath = new StringBuilder("M 0,0");
                    captureObject = new DevFacet.Geometry(position.X, position.Y, capturePath.ToString());
                    captureDraw = AddGeometry((DevFacet.Geometry)captureObject);
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
                        var pos = position - new Point(captureDraw!.X, captureDraw.Y);
                        capturePath!.Append(String.Format(" L {0},{1}", (int)pos.X, (int)pos.Y));
                        (captureDraw as DrawGeometry)?.SetPath(capturePath.ToString());
                        captureCloseable = true;
                    }
                    return true;
                case CapturePointMode.Arrow:
                    {
                        var pos = position - new Point(captureDraw!.X, captureDraw.Y);
                        capturePath!.Clear();
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
                        var pos = position - new Point(captureDraw!.X, captureDraw.Y);
                        capturePath!.Clear();
                        capturePath.Append(String.Format("M 0,0 A 1,1 180 1 1 0,{0} M 0,0 A 1,1 180 1 0 0,{0}", (int)pos.X, (int)pos.Y));
                        (captureDraw as DrawGeometry)?.SetPath(capturePath.ToString());
                        captureCloseable = true;
                    }
                    return false;
                case CapturePointMode.Rectangle:
                    {
                        var pos = position - new Point(captureDraw!.X, captureDraw.Y);
                        capturePath!.Clear();
                        capturePath.Append(String.Format("M 0,0 H {0} V {1} H 0 Z", (int)pos.X, (int)pos.Y));
                        (captureDraw as DrawGeometry)?.SetPath(capturePath.ToString());
                        captureCloseable = true;
                    }
                    return false;
                case CapturePointMode.PrintZone:
                    {
                        var pos = position - new Point(captureDraw!.X, captureDraw.Y);
                        capturePath!.Clear();
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
            var pos = position - new Point(captureDraw!.X, captureDraw.Y);

            switch (capturePointMode)
            {
                case CapturePointMode.None:
                    break;
                case CapturePointMode.Text:
                    {
                        ((DrawText)captureDraw).X = position.X;
                        ((DrawText)captureDraw).Y = position.Y;
                    }
                    break;
                case CapturePointMode.Polyline:
                    {
                        ((DrawGeometry)captureDraw)?.SetPath(capturePath + String.Format(" L {0},{1}", (int)pos.X, (int)pos.Y));
                    }
                    break;
                case CapturePointMode.Arrow:
                    {
                        ((DrawGeometry)captureDraw)?.SetPath(String.Format("M 0,0 L {0},{1}", (int)pos.X, (int)pos.Y));
                    }
                    break;
                case CapturePointMode.Ellipse:
                    {
                        ((DrawGeometry)captureDraw)?.SetPath(String.Format("M 0,0 A 1,1 180 1 1 0,{0} M 0,0 A 1,1 180 1 0 0,{0}", (int)pos.X, (int)pos.Y));
                    }
                    break;
                case CapturePointMode.Rectangle:
                    {
                        ((DrawGeometry)captureDraw)?.SetPath(String.Format("M 0,0 H {0} V {1} H 0 Z", (int)pos.X, (int)pos.Y));
                    }
                    break;
            }
        }

        private void StopCapturePositions(bool cancel)
        {
            if (cancel == false && captureCloseable == true)
            {
                if (captureDraw is DrawGeometry && captureObject is DevFacet.Geometry)
                {
                    CommandsService.Run(
                        "create geometry",
                        () => {
                            using (DevFacet.Recorder.Rec(this.Name, this.Facet!))
                            {
                                var obj = (DevFacet.Geometry)captureObject;
                                obj.X = Canvas.GetLeft(captureDraw);
                                obj.Y = Canvas.GetTop(captureDraw);
                                obj.path = capturePath!.ToString();
                                Facet!.Geometries.Add(obj);
                            }
                        }
                    ).Wait();
                }
                if (captureDraw is DrawText && captureObject is DevFacet.Text)
                {
                    var wnd = new GetText();
                    wnd.Value = "Texte";
                    wnd.Owner = Window.GetWindow(this);

                    if (wnd.ShowDialog() == true && String.IsNullOrWhiteSpace(wnd.Value) == false)
                    {
                        CommandsService.Run(
                            "create text",
                            () => {
                                using (DevFacet.Recorder.Rec(this.Name, this.Facet!))
                                {
                                    var obj = (DevFacet.Text)captureObject;
                                    obj.X = Canvas.GetLeft(captureDraw);
                                    obj.Y = Canvas.GetTop(captureDraw);
                                    obj.text = wnd.Value;
                                    ((DrawText)captureDraw).SetText(wnd.Value);
                                    Facet!.Texts.Add(obj);
                                }
                            }
                        ).Wait();
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
            captureObject = null;
            capturePath = null;
            isPointing = false;
            captureCloseable = false;

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
                CommandsService.Run(
                    "move object",
                    () => {
                        using (DevFacet.Recorder.Rec(this.Name, this.Facet!))
                        {
                            // importe l'objet
                            var name = "new";
                            if (Program.DevObject.References.ContainsKey(name) == true)
                                Program.DevObject.MakeUniqueName(ref name);

                            try
                            {
                                DevObject._checkLock.Wait();

                                using (DevObject.Recorder.New(name, obj))
                                    Program.DevObject.References.Add(name, obj.content);
                            }
                            finally
                            {
                                DevObject._checkLock.Release();
                            }


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
                                Program.Logger.WriteLine(ex2.Message);
                            }

                            // ajoute à la facette
                            var pos = Mouse.GetPosition(MyCanvas);
                            pos.X -= _translateTransform.X;
                            pos.Y -= _translateTransform.Y;
                            var props = new DevFacet.ObjectProperties { title = TitlePlacement.TopLeft, background = "#FFFFFFFF", zone = new Rect(pos, new Size(100, 100)) };
                            Facet!.Objects.Add(name, props);
                            AddElement(name, props);

                            // associe le nouvel objet à la selection
                            // sélectionne le pointeur qui correspond aux tags de l'objet
                            if (selectedElement is DrawElement)
                            {
                                try
                                {
                                    DevObject._checkLock.Wait();

                                    if (DevObject.References.TryGetValue(selectedElement.Name, out var src))
                                    {
                                        try
                                        {
                                            using (DevObject.Recorder.Rec(name, obj))
                                            {
                                                var ptr = obj.content.Pointers.First(pp => src.Tags.ContainsAll(pp.Value.tags));
                                                ptr.Value.target = selectedElement.Name;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Program.Logger.WriteLine(ex.Message);
                                        }
                                    }
                                }
                                finally
                                {
                                    DevObject._checkLock.Release();
                                }

                            }

                            try
                            {
                                DevObject._executeLock.Wait();

                                try
                                {
                                    DevObject._checkLock.Wait();

                                    Program.DevObject.CompilObjects([obj.content]);
                                    Program.DevObject.Init();// initialise les objets qui ne le sont pas encore
                                    Program.DevObject.Build(Program.DevObject.References.Where(p => p.Key == name));// construit le nouvel objet
                                }
                                finally
                                {
                                    DevObject._checkLock.Release();
                                }
                            }
                            finally
                            {
                                DevObject._executeLock.Release();
                            }

                            GuiService.InvalidateFacets();
                        }
                    }
                ).Wait();
            }
        }

        private void MenuItem_CommandsParse_Click(object sender, RoutedEventArgs e)
        {
            Program.ParseCommands(Clipboard.GetText());
        }

        internal void SetPrintZone(Rect rect)
        {
            Facet!.PrintLayout = rect;
            OnPropertyChange(nameof(PrintW));
            OnPropertyChange(nameof(PrintH));
            OnPropertyChange(nameof(PrintX));
            OnPropertyChange(nameof(PrintY));
        }
    }
}
