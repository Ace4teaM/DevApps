using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static Community.CsharpSqlite.Sqlite3;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerWindow.xaml
    /// </summary>
    public partial class DesignerWindow : Window, INotifyPropertyChanged
    {
        internal string statusText { get; set; }
        public string StatusText { get => statusText; set { statusText = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StatusText")); } }

        public event PropertyChangedEventHandler? PropertyChanged;

        public new object Content
        {
            get
            {
                return this.content.Content;
            }
            set
            {
                this.content.Content = value;
            }
        }

        public IEnumerable<TabItem> FacettesTabItems
        {
            get
            {
                return Program.DevFacet.References.Select(p => new TabItem { Header = p.Key, Tag = p.Value });
            }
        }

        public DesignerWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            StatusText = "Ready";
        }


        private void Settings_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var wnd = new App.ExternalEditors();
            wnd.Owner = Window.GetWindow(this);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            wnd.ShowDialog();
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
                                    Program.DevObject.MakeUniqueName(ref name);
                                Program.DevObject.References.Add(name, o.Value.content);
                            }

                            foreach (var o in proj.Facets)
                            {
                                var name = o.Key;
                                if (Program.DevFacet.References.ContainsKey(name) == true)
                                    Program.DevFacet.MakeUniqueName(ref name);
                                Program.DevFacet.References.Add(name, o.Value.content);
                            }

                            Service.InvalidateFacets();
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
                AddRecursiveSharedMenu(Program.CommonDataDir, m);
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
            Program.DevObject.Build();
        }

        internal void InvalidateFacets()
        {
            tabFacettes.Children.Clear();
            foreach (var item in FacettesTabItems)
            {
                var tab = new TabItem { Header = item.Header, Tag = item.Tag, Cursor = Cursors.Hand };
                tab.MouseLeftButtonUp += Tab_MouseLeftButtonUp;
                tab.MouseRightButtonUp += Tab_MouseRightButtonUp;
                tabFacettes.Children.Add(tab);
            }
        }

        private void Tab_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var tab = (sender as TabItem);
            foreach (var item in tabFacettes.Children.OfType<TabItem>())
            {
                item.Background = null;
            }
            tab.Background = Brushes.BlueViolet;
            this.Content = new DesignerView(Program.DevFacet.References.First(p=>p.Key == tab.Header.ToString()).Value);
        }

        private void Tab_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ContextMenu menu = new ContextMenu();
            var m = new MenuItem { Header = "Supprimer" };
            m.Click += (s, e) =>
            {
                var tab = (sender as TabItem);
                tabFacettes.Children.Remove(tab);

                Program.DevFacet.References.Remove(tab.Header.ToString());

                this.Content = new UserControl();
            };
            menu.Items.Add(m);
            menu.Placement = PlacementMode.Mouse;
            menu.IsOpen = true;
        }

        private void Objects_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Content = new DesignerDataView();
            foreach (var item in tabFacettes.Children.OfType<TabItem>())
            {
                item.Background = null;
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
            }
        }
    }
}
