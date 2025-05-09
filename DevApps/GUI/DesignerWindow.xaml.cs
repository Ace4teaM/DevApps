﻿using DevApps.App;
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
using System.Windows.Media;

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
            ContextMenu menu = new ContextMenu();

            var m = new MenuItem { Header = "Applications externes..." };
            m.Click += (s, e) =>
            {
                var wnd = new App.ExternalEditors();
                wnd.Owner = Window.GetWindow(this);
                wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                wnd.ShowDialog();
            };
            menu.Items.Add(m);

            m = new MenuItem { Header = "Outils externes..." };
            m.Click += (s, e) =>
            {
                var wnd = new App.ExternalTools();
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

            if (Service.IsObjectsView)
            {
                Console.WriteLine("Construit tous les objets...");
                Program.DevObject.Build();
                Console.WriteLine("Terminé");
            }
            else if (Service.IsFacetsView)
            {
                Console.WriteLine("Construit la facette active...");
                var facet = Service.GetSelectedFacet();
                facet.Build();
                Console.WriteLine("Terminé");
            }
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

        private void Variables_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Content = new DesignerVariablesView();
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
    }
}
