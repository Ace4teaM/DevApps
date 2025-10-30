using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DevApps.GUI
{
    public class FileSystemItem
    {
        public string FullPath { get; set; }
        public string Name => Path.GetFileName(FullPath);
        public ObservableCollection<FileSystemItem> Children { get; set; }

        public bool IsDirectory => Directory.Exists(FullPath);

        public FileSystemItem(string path, bool lazyLoad = true)
        {
            FullPath = path;
            Children = new ObservableCollection<FileSystemItem>();

            // Ajouter un faux enfant pour afficher la flèche d’expansion
            if (lazyLoad && IsDirectory)
                Children.Add(null);
        }

        // Chargement paresseux
        public void LoadChildren()
        {
            if (!IsDirectory) return;
            Children.Clear();

            try
            {
                foreach (var dir in Directory.GetDirectories(FullPath))
                {
                    if (AcceptFilename(dir))
                        Children.Add(new FileSystemItem(dir));
                }

                foreach (var file in Directory.GetFiles(FullPath))
                {
                    if (AcceptFilename(file))
                        Children.Add(new FileSystemItem(file, lazyLoad: false));
                }
            }
            catch { /* ignorer dossiers non accessibles */ }
        }

        public bool AcceptFilename(string name)
        {
            return Path.GetFileName(name).StartsWith(".") == false;
        }
    }

    /// <summary>
    /// Logique d'interaction pour FilesExplorerControl.xaml
    /// </summary>
    public partial class FilesExplorerControl : UserControl
    {
        public FilesExplorerControl()
        {
            InitializeComponent();

            string rootPath = Environment.CurrentDirectory;
            var rootItem = new FileSystemItem(rootPath);
            FileTree.Items.Add(rootItem);
        }

        private void FileTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FileSystemItem item)
                Console.WriteLine($"Sélectionné : {item.FullPath}");
        }
        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem tvi && tvi.DataContext is FileSystemItem item)
            {
                // Lazy-load
                if (item.Children.Count == 1 && item.Children[0] == null)
                    item.LoadChildren();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            FileTree.Dispatcher.InvokeAsync(() =>
            {
                FileTree.UpdateLayout();

                if (FileTree.ItemContainerGenerator.ContainerFromIndex(0) is TreeViewItem firstItem)
                    firstItem.IsExpanded = true;
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }
}
