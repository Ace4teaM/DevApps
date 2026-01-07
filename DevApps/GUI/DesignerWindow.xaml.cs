using Microsoft.Win32;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using static Program;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerWindow.xaml
    /// </summary>
    public partial class DesignerWindow : Window, INotifyPropertyChanged
    {
        internal string statusText = "Ready";
        public string StatusText { get => statusText; set { statusText = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusText))); } }
        public string SendButtonText
        {
            get
            {
                if (AI.Service.IsRunning == false)
                    return "Envoyer";
                else
                    return "Annuler";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChange(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public new object Content
        {
            get
            {
                return this.content.Content;
            }
            set
            {
                this.content.Content = value;
                OnPropertyChange(nameof(IsFilesView));
                OnPropertyChange(nameof(IsCommandsView));
                OnPropertyChange(nameof(IsObjectsView));
                OnPropertyChange(nameof(IsVariablesView));
                OnPropertyChange(nameof(IsDesignerView));
                OnPropertyChange(nameof(IsFacetsOrDesignerView));
                OnPropertyChange(nameof(IsFacetsView));
                OnPropertyChange(nameof(IsWelcomeView));
                OnPropertyChange(nameof(TogglePrintZone));

                if (IsWelcomeView)
                    HidePanel();
                else
                    ShowLeftPanel();
            }
        }

#region PanelVisibility
        private GridLength _savedSplitWidth = GridLength.Auto;
        private GridLength _savedRightWidth = new GridLength(3, GridUnitType.Star);

        private void HidePanel()
        {
            _savedRightWidth = rightColumn.Width;
            _savedSplitWidth = splitColumn.Width;
            rightColumn.Width = new GridLength(0);
            splitColumn.Width = new GridLength(0);
            splitter.Visibility = Visibility.Collapsed;
            rightPanel.Visibility = Visibility.Collapsed;
            InvalidateMeasure();
        }

        private void ShowLeftPanel()
        {
            rightColumn.Width = _savedRightWidth;
            splitColumn.Width = _savedSplitWidth;
            splitter.Visibility = Visibility.Visible;
            rightPanel.Visibility = Visibility.Visible;
            InvalidateMeasure();
        }
        #endregion

        public class FacetItem
        {
            public string Header { get; set; }
            internal Program.DevFacet Tag { get; set; }

            internal FacetItem(string header, Program.DevFacet tag)
            {
                Header = header;
                Tag = tag;
            }
        }

        public IEnumerable<FacetItem> FacetItems
        {
            get
            {
                return Program.DevFacet.References.Select(p=>new FacetItem (  header: p.Key, tag: p.Value ));
            }
        }

        public class ObjectModel
        {
            public required string Header { get; set; }
            public required string Key { get; set; }
            public required string Filename { get; set; }
            public string Dirname { get {  return Path.GetDirectoryName(Filename) ?? string.Empty; } }
            internal Serializer.DevObjectInstance Value { get; set; }
        }

        public FacetItem? SelectedFacet
        {
            get;set;
        }

        List<ObjectModel>? objectModels = null;
        public IEnumerable<ObjectModel> ObjectModels
        {
            get
            {
                if (objectModels == null)
                {
                    objectModels = new List<ObjectModel>();
                    AddRecursiveSharedModelObjects(Program.CommonSharedPath, objectModels);
                    objectModels.Sort((a,b)=>a.Header.CompareTo(b.Header));
                }
                return objectModels;
            }
        }
        public ObjectModel? SelectedObjectModel
        {
            get;set;
        }

        public static Version? AppVersion
        {
            get
            {
                return Assembly.GetEntryAssembly()?.GetName().Version;
            }
        }

        public string AppTitle
        {
            get
            {
                return Path.GetFileName(Environment.CurrentDirectory);
            }
        }

        public string AppPath
        {
            get
            {
                return Environment.CurrentDirectory;
            }
        }

        Application app = new Application();

        public DesignerWindow()
        {
            try
            {
                // Charger les dictionnaires WPF-UI
                // https://github.com/dotnet/wpf/blob/e16222f888d89a3c06efd8fb252f67ed30f39050/src/Microsoft.DotNet.Wpf/src/Themes/PresentationFramework.Fluent/Themes/Fluent.Dark.xaml#L513-L518
                var themeDictionary = new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.xaml")
                };

                // Ajouter aux ressources globales
                app.Resources.MergedDictionaries.Add(themeDictionary);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors du chargement des ressources : " + ex.Message);
            }

            InitializeComponent();
            this.DataContext = this;

            AI.Service.MessageReceived += Service_MessageReceived;
        }

        private void Service_MessageReceived(object? sender, EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SendButtonText)));
        }

        private void Settings_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ContextMenu menu = new ContextMenu();

            var m = new MenuItem { Header = "Applications externes..." };
            m.Click += (s, e) =>
            {
                var wnd = new Appli.ExternalEditors();
                wnd.Owner = Window.GetWindow(this);
                wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                wnd.ShowDialog();
            };
            menu.Items.Add(m);

            m = new MenuItem { Header = "Outils externes..." };
            m.Click += (s, e) =>
            {
                var wnd = new Appli.ExternalTools();
                wnd.Owner = Window.GetWindow(this);
                wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                wnd.ShowDialog();
            };
            menu.Items.Add(m);

            m = new MenuItem { Header = "Profil IA..." };
            m.Click += (s, e) =>
            {
                var wnd = new Appli.AiProfile();
                wnd.Owner = Window.GetWindow(this);
                wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                wnd.ShowDialog();
            };
            menu.Items.Add(m);

            m = new MenuItem { Header = "Définir le raccourci dans le menu contextuel Windows" };
            m.Click += (s, e) =>
            {
                try
                {
                    var registryKey = @"Software\DevAppsSetup";

                    using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(registryKey))
                    {
                        if (key != null)
                        {
                            var path = key.GetValue(null, null)?.ToString();
                            if(path == null)
                            {
                                Console.WriteLine("DevAppsSetup n'est pas installé ou n'est pas enregistré au registre");
                                Console.WriteLine("Veuillez d'abord executer DevAppsSetup.exe");
                                return;
                            }
                            else
                            {
                                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, Arguments = "--add-shell" });
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erreur : " + ex.Message);
                }
            };
            menu.Items.Add(m);

            menu.Placement = PlacementMode.Mouse;
            menu.IsOpen = true;
        }

        private void AddRecursiveSharedModelObjects(string path, List<ObjectModel> list)
        {
            try
            {
                // liste les objets partagés
                foreach (var dir in Directory.EnumerateDirectories(path))
                {
                    var filename = System.IO.Path.Combine(dir, Program.Filename);
                    if (File.Exists(filename) == true)
                    {
                        using StreamReader reader = new StreamReader(filename);

                        JsonSerializer serializer = JsonSerializer.CreateDefault();
                        serializer.Error += (sender, e) =>
                        {
                            System.Console.WriteLine(e.ErrorContext.Error.ToString());
                        };

                        var proj = new Serializer.DevExternalProject();

                        serializer.Populate(reader, proj);

                        // Ajoute les objets à la liste

                        foreach (var o in proj.Objects)
                        {
                            if(o.Value.Guid != null)
                                list.Add(new ObjectModel { Header = o.Value.Description ?? o.Key, Key = o.Key, Value = o.Value, Filename = filename });
                        }
                    }
                    else
                    {
                        AddRecursiveSharedModelObjects(dir, list);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void AddRecursiveSharedMenu(string path, MenuItem menu)
        {
            try
            {
                // liste les objets partagés
                foreach (var dir in Directory.EnumerateDirectories(path))
                {
                    var filename = System.IO.Path.Combine(dir, Program.Filename);
                    if (File.Exists(filename) == true)
                    {
                        var m = new MenuItem { Header = System.IO.Path.GetFileName(dir) };
                        m.Click += (s, e) =>
                        {
                            using StreamReader reader = new StreamReader(filename);

                            JsonSerializer serializer = JsonSerializer.CreateDefault();
                            serializer.Error += (sender, e) =>
                            {
                                System.Console.WriteLine(e.ErrorContext.Error.ToString());
                            };

                            var proj = new Serializer.DevExternalProject();

                            serializer.Populate(reader, proj);

                            // Ajoute les objets au projet

                            foreach(var o in proj.Objects)
                            {
                                var name = o.Key;
                                if (Program.DevObject.References.ContainsKey(name) == true)
                                {
                                    Program.DevObject.MakeUniqueName(ref name, proj.Objects.Select(p=>p.Key)); // pas de conflit non plus avec d'autres objets du projet en cours d'importation

                                    // Actualise les pointeurs
                                    foreach (var o2 in proj.Objects)
                                    {
                                        foreach (var ptr in o2.Value.content.Pointers)
                                        {
                                            if (String.Compare(ptr.Value.target, o.Key, true) == 0)
                                                ptr.Value.target = name;
                                        }
                                    }

                                    // Actualise les noms dans les facettes
                                    foreach (var f in proj.Facets)
                                    {
                                        foreach (var o2 in f.Value.content.Objects.ToArray())
                                        {
                                            if (String.Compare(o2.Key, o.Key, true) == 0)
                                            {
                                                f.Value.content.Objects[name] = o2.Value;
                                                f.Value.content.Objects.Remove(o.Key);
                                            }
                                        }
                                    }
                                }

                                // Conserve le guid de base
                                o.Value.content.baseGuid = o.Value.content.guid;
                                o.Value.content.guid = null;

                                Program.DevObject.References.Add(name, o.Value.content);

                                // importe les données
                                try
                                {
                                    if (String.IsNullOrEmpty(o.Value.InitialDataBase64) == false)
                                    {
                                        var data = Convert.FromBase64String(o.Value.InitialDataBase64);
                                        o.Value.content.buildStream.Seek(0, SeekOrigin.Begin);
                                        o.Value.content.buildStream.Write(data);
                                        o.Value.content.buildStream.SetLength(data.Length);
                                    }
                                }
                                catch (Exception ex2)
                                {
                                    Console.WriteLine(ex2.Message);
                                }
                            }

                            foreach (var o in proj.Facets)
                            {
                                var name = o.Key;
                                if (Program.DevFacet.References.ContainsKey(name) == true)
                                    Program.DevFacet.MakeUniqueName(ref name);
                                Program.DevFacet.References.Add(name, o.Value.content);
                            }

                            Program.DevObject.CompilObjects(proj.Objects.Select(p=>p.Value.content));
                            Program.DevObject.Init();// initialise les objets qui ne le sont pas encore
                            GuiService.InvalidateFacets();
                        };
                        menu.Items.Add(m);
                    }
                    else
                    {
                        var m = new MenuItem { Header = System.IO.Path.GetFileName(dir) };
                        menu.Items.Add(m);
                        AddRecursiveSharedMenu(dir, m);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void Menu_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // liste les objets partagés
                ContextMenu menu = new ContextMenu();
                var m = new MenuItem { Header = "Shared models" };
                AddRecursiveSharedMenu(Program.CommonSharedPath, m);
                menu.Items.Add(m);
                menu.Placement = PlacementMode.Top;
                menu.PlacementTarget = sender as UIElement;
                menu.IsOpen = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void Build_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(Program.DevObject.References.Count == 0)
            {
                MessageBox.Show("Aucun objet à construire !", "Build", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (GuiService.IsObjectsView)
            {
                Console.WriteLine("Construit tous les objets...");
                Program.DevObject.Build();
                Console.WriteLine("Terminé");
            }
            else if (GuiService.IsFacetsView)
            {
                Console.WriteLine("Construit la facette active...");
                var facet = GuiService.GetSelectedFacet();
                if(facet != null)
                {
                    facet.Build();
                    Console.WriteLine("Terminé");
                }
            }
        }

        internal void InvalidateFacets()
        {
            OnPropertyChange(nameof(FacetItems));
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ((ListBox)sender).SelectedItem as FacetItem;
            if(item != null)
            {
                this.Content = new DesignerView(Program.DevFacet.References.First(p => p.Key == item.Header.ToString()).Value);
            }
        }
        private void MenuItem_Click_DeleteFacet(object sender, RoutedEventArgs e)
        {
            if (SelectedFacet != null)
            {
                Program.DevFacet.References.Remove(SelectedFacet.Header.ToString());
            }

            this.Content = new UserControl();

            OnPropertyChange(nameof(FacetItems));
        }

        private void MenuItem_Click_RenameFacet(object sender, RoutedEventArgs e)
        {
            if (SelectedFacet != null)
            {
                var view = new NewFacette();
                view.Owner = this;
                view.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                view.Title = "Rename facet...";
                if (view.ShowDialog() == true)
                {
                    var item = SelectedFacet.Tag;
                    Program.DevFacet.References.Remove(SelectedFacet.Header.ToString());
                    Program.DevFacet.References.Add(view.Value, item);

                    // sélectionne de nouveau l'item
                    FacetListBox.SelectedIndex = FacetListBox.Items.IndexOf(FacetListBox.Items.OfType<FacetItem>().First(p=>p.Tag == item));
                }
            }

            OnPropertyChange(nameof(FacetItems));
        }

        private void Commands_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            SelectedFacet = null;
            OnPropertyChange(nameof(SelectedFacet));

            this.Content = new DesignerCommandsView();

            e.Handled = true;
        }

        private void Objects_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            SelectedFacet = null;
            OnPropertyChange(nameof(SelectedFacet));

            this.Content = new DesignerDataView();

            e.Handled = true;
        }

        private void Facets_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            if(FacetListBox.Items.Count == 0)
            {
                this.Content = new DesignerFacetsView();
            }
            else
            {
                FacetListBox.SelectedIndex = 0;
                OnPropertyChange(nameof(SelectedFacet));
            }
            e.Handled = true;
        }

        private void Variables_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            SelectedFacet = null;
            OnPropertyChange(nameof(SelectedFacet));

            this.Content = new DesignerVariablesView();
            e.Handled = true;
        }

        private void Files_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            SelectedFacet = null;
            OnPropertyChange(nameof(SelectedFacet));

            this.Content = new DesignerFilesView();
            e.Handled = true;
        }
        public bool IsFilesView
        {
            get
            {
                return this.Content is DesignerFilesView;
            }
        }

        public bool IsObjectsView
        {
            get
            {
                return this.Content is DesignerDataView;
            }
        }

        public bool IsCommandsView
        {
            get
            {
                return this.Content is DesignerCommandsView;
            }
        }

        public bool IsVariablesView
        {
            get
            {
                return this.Content is DesignerVariablesView;
            }
        }

        public bool IsDesignerView
        {
            get
            {
                return this.Content is DesignerView;
            }
        }

        public bool IsFacetsView
        {
            get
            {
                return this.Content is DesignerFacetsView;
            }
        }

        public bool IsWelcomeView
        {
            get
            {
                return this.Content is WelcomeView;
            }
        }

        public bool IsFacetsOrDesignerView
        {
            get
            {
                return IsFacetsView || IsDesignerView;
            }
        }

        private void Add_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var wnd = new NewFacette();
            wnd.Owner = Window.GetWindow(this);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (wnd.ShowDialog() == true)
            {
                Program.DevFacet.Create(wnd.Value, []);
                InvalidateFacets();
                FacetListBox.SelectedIndex = FacetListBox.Items.Count-1;
            }
        }

        private ModifierKeys lastModifier = ModifierKeys.None;

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(Content is IKeyCommand)
            {
                var kc = (Content as IKeyCommand);

                if (Keyboard.Modifiers != lastModifier)
                {
                    kc?.OnKeyState(Keyboard.Modifiers);
                }

                lastModifier = Keyboard.Modifiers;

                switch (e.Key)
                {
                    case Key.Escape:
                        kc?.OnKeyCommand(KeyCommand.Cancel);
                        break;
                    case Key.Left:
                        kc?.OnKeyCommand(KeyCommand.MoveLeft);
                        break;
                    case Key.Right:
                        kc?.OnKeyCommand(KeyCommand.MoveRight);
                        break;
                    case Key.Up:
                        kc?.OnKeyCommand(KeyCommand.MoveTop);
                        break;
                    case Key.Down:
                        kc?.OnKeyCommand(KeyCommand.MoveBottom);
                        break;
                    case Key.Insert:
                        kc?.OnKeyCommand(KeyCommand.Create);
                        break;
                    case Key.Delete:
                        kc?.OnKeyCommand(KeyCommand.Delete);
                        break;
                }
            }

            // Sauvegarde du projet
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                // Sauvegarde les données permanentes
                DevObject.SaveOutput();

                Program.SaveProject();
            }

            // Imprime le projet
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.P)
            {
                if (IsDesignerView)
                {
                    DevApps.Print.Services.Print(((DesignerView)this.Content).facette);
                }
            }

            // Imprime le projet
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.P)
            {
                DevApps.Print.Services.PrintAll();
            }
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (Content is IKeyCommand)
            {
                var kc = (Content as IKeyCommand);

                if (Keyboard.Modifiers != lastModifier)
                {
                    kc?.OnKeyState(Keyboard.Modifiers);
                }
            }

            lastModifier = Keyboard.Modifiers;
        }

        private void IASendPopup_Click(object sender, RoutedEventArgs e)
        {
            if(AI.Service.IsRunning)
            {
                AI.Service.Cancel();
            }
            else
            {
                AI.Service.Send(PopupInput.Text);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SendButtonText)));
        }

        private void IA_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ChatPopup.IsOpen = true;
        }

        private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else if (e.ButtonState == MouseButtonState.Pressed && e.ClickCount == 1)
            {
                DragMove();
            }
            e.Handled = true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
            e.Handled = true;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            e.Handled = true;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            e.Handled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Content = new WelcomeView();
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            var over = e.MouseDevice.DirectlyOver;
            var item = ObjectListBox.SelectedItem;
            if (item != null && e.LeftButton == MouseButtonState.Pressed)
            {
                if (over is FrameworkElement element && element.DataContext is DesignerWindow.ObjectModel)
                    DragDrop.DoDragDrop(ObjectListBox,
                                     element.DataContext,
                                     DragDropEffects.Copy);
            }
        }

        private void MenuItem_Click_OpenParentProject(object sender, RoutedEventArgs e)
        {
            var item = ObjectListBox.SelectedItem as DesignerWindow.ObjectModel;
            if (item != null)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(Path.Combine(Program.ExecutablePath, "DevApps.exe")) { WorkingDirectory = item.Dirname, Arguments = "-w" });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true, Arguments = AppPath });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void Simulation_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DevObject.IsRunning)
            {
                DevObject.Stop();
            }
            else
            {
                DevObject.Start();
            }
        }

        public string StartStopIcon
        {
            get
            {
                return DevObject.IsRunning ? "⏹" : "⏵";
            }
        }

        public string StartStopText
        {
            get
            {
                return DevObject.IsRunning ? "Arrêter la simulation" : "Démarrer la simulation";
            }
        }

        public void WorkerChange()
        {
            OnPropertyChange(nameof(StartStopIcon));
            OnPropertyChange(nameof(StartStopText));
        }

        public bool TogglePrintZone
        {
            get
            {
                return IsDesignerView && ((DesignerView)Content)?.PrintVisibility == true ? true : false;
            }
            set
            {
                if(IsDesignerView)
                    ((DesignerView)Content).PrintVisibility = value;
                OnPropertyChange(nameof(TogglePrintZone));
            }
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            TogglePrintZone = true;
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            TogglePrintZone = false;
        }

        private void DropdownBtn_Click(object sender, RoutedEventArgs e)
        {
            DropdownPopup.IsOpen = !DropdownPopup.IsOpen;
        }

        private void PrintSize_Click(object sender, RoutedEventArgs e)
        {
            if (IsDesignerView)
            {
                var view = ((DesignerView)Content)!;

                var formats = new Dictionary<string, (double L, double H)>
                {
                    { "Portrait", (841, 1189) }, //A*
                    { "Paysage", (1189, 841) } //A*
                };

                Rect rect = new Rect();

                if(formats.TryGetValue(((Button)sender).Tag.ToString()!, out var size))
                {
                    rect = view.GetObjectsBounding();

                    var ratio = (1.0 / size.L) * size.H;
                    var newHeight = ratio * rect.Width;
                    // si on ajuste la hauteur pour ce ratio de largeur
                    // a t'on suffisament pour contenir le tout ? (si positif = plus grand)
                    if (newHeight >= rect.Height)
                    {
                        var diff = newHeight - rect.Height;
                        rect.Height = newHeight;

                        rect.Y -= diff / 2.0;
                    }
                    else
                    {
                        // sinon il faut ajuster la largeur
                        ratio = (1.0 / size.H) * size.L;
                        var newWidth = ratio * rect.Height;

                        var diff = newWidth - rect.Width;
                        rect.Width = newWidth;

                        rect.X -= diff / 2.0;
                    }

                    // centre le contenu
                }
                else
                {
                    rect = view.GetObjectsBounding();
                }

                view.SetPrintZone(rect);

                // Affiche la zone
                TogglePrintZone = true;
            }
            DropdownPopup.IsOpen = false;
        }
    }
}
