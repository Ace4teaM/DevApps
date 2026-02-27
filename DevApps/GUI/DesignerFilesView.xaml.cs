using Microsoft.Scripting.Utils;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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

        private void DataGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (CheckFilenames(files) == false)
                    return;

                AddFromFile(files);

            }
            if (e.Data.GetDataPresent(typeof(FileSystemItem)))
            {
                var item = (FileSystemItem)e.Data.GetData(typeof(FileSystemItem));

                if (CheckFilenames(item.FullPath) == false)
                    return;

                AddFromFile(item.FullPath);
            }
        }

        private static bool CheckFilenames(params string[] filenames)
        {
            foreach (var filename in filenames)
            {
                var path = Path.GetFullPath(filename);
                var data = Path.GetFullPath(DataDir);
                var wdir = Environment.CurrentDirectory;

                if (String.IsNullOrWhiteSpace(path) == true)
                {
                    MessageBox.Show("Le nom de fichier n'est pas valide", "Emplacement invalide", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return false;
                }
                if (path.StartsWith(data) == true)
                {
                    MessageBox.Show("Le fichier ne peut pas être dans le répertoire du cache", "Emplacement invalide", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return false;
                }
                if (path.StartsWith(wdir) == false)
                {
                    MessageBox.Show("Le fichier ne peut pas être en dehors du répertoire de travail", "Emplacement invalide", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return false;
                }
                if (Path.GetFileName(path) == Filename)
                {
                    MessageBox.Show("Le fichier ne peut pas être le fichier du projet", "Emplacement invalide", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return false;
                }
            }

            return true;
        }

        private void AddFromFile(params string[] filenames)
        {
            try
            {
                DevObject._executeLock.Wait();

                try
                {
                    DevObject._checkLock.Wait();

                    foreach (var filename in filenames)
                    {
                        var name = Path.GetFileNameWithoutExtension(filename);
                        Program.DevObject.MakeUniqueName(ref name, null);

                        // Crée l'objet
                        var obj = new DevObjectFile(Path.Combine(DataDir, name));

                        var ext = Path.GetExtension(filename);
                        if (ext.Length > 1)
                            obj.tags = new HashSet<string>([ext.Substring(1)]);

                        // Détermine le dessin de l'objet en fonction des tags
                        obj.drawCode = DevObject.DrawCodeFromExt(ext);

                        // Crée la référence vers le fichier
                        var file = new DevFile(Path.GetRelativePath(Environment.CurrentDirectory, filename), name);

                        obj.Description = file.Filename;

                        // Ajoute aux références
                        Program.DevFile.References.Add(file.filename, file);
                        Program.DevObject.References.Add(name, obj);

                        // Copie le contenu du fichier
                        if (File.Exists(file.Filename) && obj.filename != null)
                        {
                            File.Copy(file.Filename, obj.filename, true);
                            obj.LoadContent();
                        }

                        Program.DevObject.CompilObjects([obj]);
                    }

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
            InvalidateItems();
        }


        private void CreateObject()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Fichiers (*.*)|*.*"
            };

            bool ok = false;
            do{
                dlg.InitialDirectory = Environment.CurrentDirectory;

                if (dlg.ShowDialog() != true)
                    break;

                var path = Path.GetFullPath(dlg.FileName);
                if (CheckFilenames(path) == false)
                    continue;

                ok = true;
            } while (ok == false);

            // ouvre l'explorateur de fichiers
            if(ok)
            {
                AddFromFile(dlg.FileName);
            }
        }

        private void DeleteObject()
        {
            var selection = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Filename ?? String.Empty).ToArray();

            foreach (var name in selection)
            {
                if (Program.DevFile.References.TryGetValue(name, out var file))
                {
                    Program.DevFile.References.Remove(name);
                    file.Dispose();

                    try
                    {
                        DevObject._executeLock.Wait();

                        try
                        {
                            DevObject._checkLock.Wait();

                            if (Program.DevObject.TryGet(file.objectname, out var obj))
                            {
                                Program.DevObject.DeleteObject(file.objectname);
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
                }
            }

            InvalidateItems();
        }

        private void ExecuteObject()
        {
            var selection = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Filename ?? String.Empty).ToArray();

            try
            {
                DevObject._executeLock.Wait();

                try
                {
                    DevObject._checkLock.Wait();

                    foreach (var name in selection)
                    {
                        if (Program.DevFile.References.TryGetValue(name, out var file))
                        {
                            // différent ?
                            if (file.Diff() == true)
                            {
                                file.Write();
                            }
                            else
                            {
                                Program.Logger.WriteLine("Pas de difference entre les fichiers.");
                            }
                        }
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
            var path = Path.GetFullPath(newFilename);
            if (CheckFilenames(path) == false)
                return false;

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
                Program.Logger.WriteLine(ex.Message);
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
                    var textBox = e.EditingElement as TextBox;
                    var text = textBox?.Text;
                    if (text != null && textBox != null && item != null)
                    {
                        try
                        {
                            Program.DevFile._checkLock.Wait();

                            if (Program.DevFile.TryGet(item.Filename, out var reference))
                            {
                                if (e.Column.Header.ToString() == "Fichier")
                                {
                                    if (text != item.Filename)
                                    {
                                        if (RecreateFileEntry(item.Filename, item.ObjectName, text))
                                        {
                                            // renomme l'objet
                                            item.Filename = text;
                                        }
                                        else
                                        {
                                            // annule et rétablit l'ancienne valeur
                                            textBox.Text = item.Filename;
                                            e.Cancel = true;
                                        }
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
                            Program.Logger.WriteLine(ex.Message);
                        }
                        finally
                        {
                            Program.DevFile._checkLock.Release();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine(ex.Message);
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

        private void MenuItem_Click_ExecuteFileEntry(object sender, RoutedEventArgs e)
        {
            ExecuteObject();
        }

        public void OnKeyState(ModifierKeys modifier)
        {
        }
    }
}
