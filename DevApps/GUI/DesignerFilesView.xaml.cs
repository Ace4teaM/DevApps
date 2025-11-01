using Microsoft.Scripting.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Program;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerFilesView.xaml
    /// </summary>
    public partial class DesignerFilesView : UserControl, INotifyPropertyChanged, IKeyCommand
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool IsEditing = false;

        public class TabItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            public TabItem(string Filename)
            {
                this.filename = Filename;

                if (String.IsNullOrEmpty(Filename) || Program.DevFile.References.ContainsKey(Filename) == false)
                    throw new ArgumentException(nameof(Filename));

                this.objectname = Program.DevFile.References[Filename].objectname;
                this.filename = Program.DevFile.References[Filename].filename;
            }

            private string filename;
            public string Filename { get { return filename; } set { filename = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Filename))); } }

            internal string objectname;
            public string ObjectName
            {
                get
                {
                    return objectname;
                }
                set
                {
                    objectname = value;
                    Program.DevFile.References[Filename].objectname = objectname;//todo mutex avec callback
                }
            }
        }

        public System.Windows.Media.SolidColorBrush AccentBrush
        {
            get
            {
                return System.Windows.SystemColors.AccentColorBrush;
            }
        }

        private ObservableCollection<TabItem> items = new ObservableCollection<TabItem>();
        public ObservableCollection<TabItem> Items
        {
            get
            {
                return items;
            }
        }

        internal void InvalidateItems()
        {
            items.Clear();
            items.AddRange(Program.DevFile.References.Select(p => new TabItem(p.Key)));

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
        }

        public DesignerFilesView()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Loaded += DesignerDataView_Loaded;
        }

        private void DesignerDataView_Loaded(object sender, RoutedEventArgs e)
        {
            InvalidateItems();
        }

        private void CreateObject()
        {
            // ouvre l'explorateur de fichiers
            /*var wnd = new NewObject();
            wnd.Owner = Window.GetWindow(this);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (wnd.ShowDialog() == true)
            {
                Program.DevObject.Create(wnd.Value, String.Empty, wnd.Tags);
                InvalidateObjects();
            }*/
        }

        private void DeleteObject()
        {
            /*
            var selection = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Filename ?? String.Empty).ToArray();

            foreach (var name in selection)
            {
                if (Program.DevFile.References.ContainsKey(name))
                {
                    Program.DevFile.Delete(name);
                }
            }

            InvalidateItems();*/
        }

        public void OnKeyCommand(KeyCommand command)
        {
            if (IsEditing == true)
                return;

            if (command == KeyCommand.Create)
            {
                CreateObject();
                return;
            }
            if (command == KeyCommand.Delete)
            {
                DeleteObject();
                return;
            }
        }

        private bool RecreateFileEntry(string filename, string objectname, string newFilename)
        {
            if (String.IsNullOrEmpty(newFilename))
            {
                // recrée l'objet
                try
                {
                    var newEntry = new Program.DevFile(newFilename, objectname);

                    var existing = Program.DevFile.References[filename];
                    existing.Dispose();

                    Program.DevFile.References.Remove(filename);
                    Program.DevFile.References.Add(newFilename, newEntry);

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return false;
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            IsEditing = true;
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                if (e.EditAction == DataGridEditAction.Commit)
                {
                    var item = e.Row.DataContext as TabItem;
                    var text = (e.EditingElement as TextBox)?.Text;
                    if (text != null && item != null)
                    {
                        var handle = Program.DevFile.mutexCheckList.WaitOne();
                        if (handle)
                        {
                            try
                            {
                                Program.DevFile.References.TryGetValue(item.Filename, out var reference);

                                if (reference != null)
                                {
                                    if (e.Column.Header.ToString() == "Fichier")
                                    {
                                        if (text != item.Filename)
                                        {
                                            RecreateFileEntry(item.Filename, item.ObjectName, text);

                                            // renomme l'objet
                                            item.Filename = text;
                                        }
                                    }
                                    else if (e.Column.Header.ToString() == "Objet")
                                    {
                                        item.ObjectName = text;
                                        reference.objectname = text;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                            finally
                            {
                                Program.DevFile.mutexCheckList.ReleaseMutex();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            IsEditing = false;
        }

        private void MenuItem_Click_CreateFileEntry(object sender, RoutedEventArgs e)
        {
            CreateObject();
        }

        private void MenuItem_Click_DeleteFileEntry(object sender, RoutedEventArgs e)
        {
            DeleteObject();
        }

        private void ExecuteFileEntry()
        {
            /*var selection = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Filename ?? String.Empty).ToArray();

            foreach (var name in selection)
            {
                if (Program.DevFile.References.ContainsKey(name))
                {
                    Program.DevFile.References[name].Execute();
                }
                else
                {
                    Console.WriteLine($"Command group '{name}' not found.");
                }
            }*/
        }

        private void MenuItem_Click_ExecuteFileEntry(object sender, RoutedEventArgs e)
        {
            ExecuteFileEntry();
        }

        public void OnKeyState(ModifierKeys modifier)
        {
        }
    }
}
