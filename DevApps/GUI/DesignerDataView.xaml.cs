using DevApps.Commands;
using IronPython.Runtime;
using Microsoft.Scripting.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static IronPython.Modules._ast;
using static Program;
using static Program.DevObject;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerDataView.xaml
    /// </summary>
    public partial class DesignerDataView : UserControl, INotifyPropertyChanged, IKeyCommand, IInvalidableView
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class TabItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            public void OnPropertyChanged(string name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            public void OnAllPropertiesChanged()
            {
                OnPropertyChanged(nameof(IsPointed));
                OnPropertyChanged(nameof(IsReference));
                OnPropertyChanged(nameof(MustBeBuild));
                OnPropertyChanged(nameof(BuildIndex));
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(Tags));
                OnPropertyChanged(nameof(UserAction));
                OnPropertyChanged(nameof(LoopMethod));
                OnPropertyChanged(nameof(InitMethod));
                OnPropertyChanged(nameof(BuildMethod));
                OnPropertyChanged(nameof(DrawCode));
                OnPropertyChanged(nameof(Facettes));
                OnPropertyChanged(nameof(Selections));
                OnPropertyChanged(nameof(CanBuild));
            }
            private bool isPointed = false;
            public bool IsPointed { get { return isPointed; } set { isPointed = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPointed))); } }
            private bool isPointer = false;
            public bool IsPointer { get { return isPointer; } set { isPointer = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPointer))); } }
            public bool? IsReference
            {
                get
                {
                    var obj = Program.DevObject.References.FirstOrDefault(p => p.Key == Name).Value;
                    return obj?.IsReference;
                }
            }
            public bool? MustBeBuild
            {
                get
                {
                    var obj = Program.DevObject.References.FirstOrDefault(p => p.Key == Name).Value;
                    return obj?.MustBeBuild;
                }
            }
            public int? BuildIndex
            {
                get
                {
                    var obj = Program.DevObject.References.FirstOrDefault(p => p.Key == Name).Value;
                    return obj?.BuildIndex;
                }
            }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public string Tags { get { return tags; } set { tags = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tags))); } }
            private string tags = String.Empty;
            public string? UserAction
            {
                get
                {
                    var obj = Program.DevObject.References.FirstOrDefault(p => p.Key == Name).Value;
                    return obj?.UserAction.Item1;
                }
            }
            public string? LoopMethod
            {
                get
                {
                    var obj = Program.DevObject.References.FirstOrDefault(p => p.Key == Name).Value;
                    return obj?.LoopMethod.Item1;
                }
            }
            public string? InitMethod
            {
                get
                {
                    var obj = Program.DevObject.References.FirstOrDefault(p => p.Key == Name).Value;
                    return obj?.InitMethod.Item1;
                }
            }
            public string? BuildMethod
            {
                get
                {
                    var obj = Program.DevObject.References.FirstOrDefault(p => p.Key == Name).Value;
                    return obj?.BuildMethod.Item1;
                }
            }
            public string? DrawCode
            {
                get
                {
                    var obj = Program.DevObject.References.FirstOrDefault(p => p.Key == Name).Value;
                    return obj?.DrawCode.Item1;
                }
            }
            public string? Facettes
            {
                get
                {
                    if(Name == null)
                        return string.Empty;
                    var facettes = Program.DevFacet.References.Where(p => p.Value.Objects.Keys.Contains(Name)).Select(p => p.Key).ToList();
                    return String.Join(", ", facettes);
                }
            }
            public string? Selections
            {
                get
                {
                    return String.Empty;
                }
            }
            public bool CanBuild
            {
                get
                {
                    return BuildMethod != null && String.IsNullOrWhiteSpace(BuildMethod) == false;
                }
            }
        }

        /// <summary>
        /// true Si une cellule est en cours d'édition
        /// </summary>
        private bool IsEditing = false;

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

        public void InvalidateContent()
        {
            InvalidateObjects();
        }

        internal void InvalidateObjects()
        {
            try
            {
                DevObject._checkLock.Wait();

                items.Clear();
                items.AddRange(new ObservableCollection<TabItem>(Program.DevObject.References.Select(p => new TabItem { Name = p.Key, Description = p.Value.Description, Tags = String.Join(' ', p.Value.Tags) })));
            }
            finally
            {
                DevObject._checkLock.Release();
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
        }

        public void InvalidateObjectsStatus()
        {
            // Actualise les compteurs
            foreach (var i in Items)
            {
                i.OnAllPropertiesChanged();
            }
        }

        public void InvalidatePointerVisual()
        {
            if (dataGrid.SelectedItem is TabItem selectedItem)
            {
                try
                {
                    DevObject._checkLock.Wait();

                    if (DevObject.TryGet(selectedItem.Name, out var selectedObject))
                    {
                        selectedItem.IsPointed = false;
                        selectedItem.IsPointer = false;
                        foreach (var item in Items)
                        {
                            if (item != selectedItem && DevObject.TryGet(item.Name, out var obj))
                            {
                                item.IsPointed = selectedObject.Pointers.Count(p => p.Value.target == item.Name) > 0;//cet objet est pointé par la selection ?
                                item.IsPointer = obj.Pointers.Count(p => p.Value.target == selectedItem.Name) > 0;//cet objet pointe vers la selection ?
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.WriteLine(ex.Message);
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
            else
            {
                foreach (var item in Items)
                {
                    item.IsPointed = false;
                    item.IsPointer = false;
                }
            }
        }

        public DesignerDataView()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Loaded += DesignerDataView_Loaded;
        }

        private void DesignerDataView_Loaded(object sender, RoutedEventArgs e)
        {
            InvalidateObjects();
        }

        private void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((sender as ContentControl)?.Content as FrameworkElement);
            var content = String.Empty;
            var context = ((sender as ContentControl)?.DataContext as TabItem);

            ScriptType scriptType;
            string? scriptCode = null;

            if (item == null || context == null)
                return;

            switch (item.Name)
            {
                case "DrawCode":
                    content = context.DrawCode;
                    scriptType = ScriptType.Draw;
                    break;
                case "BuildMethod":
                    content = context.BuildMethod;
                    scriptType = ScriptType.Build;
                    break;
                case "LoopMethod":
                    content = context.LoopMethod;
                    scriptType = ScriptType.Loop;
                    break;
                case "InitMethod":
                    content = context.InitMethod;
                    scriptType = ScriptType.Init;
                    break;
                case "UserAction":
                    content = context.UserAction;
                    scriptType = ScriptType.UserAction;
                    break;
                default:
                    return;
            }

            if (context.Name != null)
            {
                // Infos
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(String.Format("{0} = {1}", "out", "output"));
                stringBuilder.AppendLine(String.Format("{0} = {1}", "name", "nom de l'objet"));
                stringBuilder.AppendLine(String.Format("{0} = {1}", "desc", "description de l'objet"));

                try
                {
                    DevObject._executeLock.Wait();

                    try
                    {
                        DevObject._checkLock.Wait();

                        if (Program.DevObject.TryGet(context.Name, out var obj))
                        {
                            if (obj.Pointers.Count > 0)
                            {
                                stringBuilder.AppendLine();
                                stringBuilder.AppendLine(String.Format("Pointeurs:"));
                                foreach (var pointer in obj.Pointers)
                                {
                                    stringBuilder.AppendLine(String.Format("{0} => [{1}]", pointer.Key, pointer.Value.target));
                                }
                            }

                            if (obj.Properties.Count > 0)
                            {
                                stringBuilder.AppendLine();
                                stringBuilder.AppendLine(String.Format("Propriétés:"));
                                foreach (var property in obj.Properties)
                                {
                                    stringBuilder.AppendLine(String.Format("{0} => [{1}]", property.Key, property.Value.Item1));
                                }
                            }

                            var wnd = new ScriptEdit(String.Format("{0} ({1})", context.Name, item.Name), content ?? string.Empty, obj.Properties);

                            wnd.Infos = stringBuilder.ToString();
                            wnd.Owner = Window.GetWindow(this);
                            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            if (wnd.ShowDialog() == true)
                            {
                                scriptCode = wnd.Value;
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

                if (scriptCode != null)
                {
                    if (CommandsService.Run(
                        "update script",
                        () => Features.Objects.SetScript(context.Name, scriptType, scriptCode)
                    ).Result == false)
                        MessageBox.Show(GuiService.EditorWindow, "Erreur de compilation.", "Compilation", MessageBoxButton.OK, MessageBoxImage.Exclamation); //todo get last error message
                }
            }
        }

        private void CreateFacet()
        {
            var selection = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Name ?? String.Empty).ToArray();

            var wnd = new NewFacette();
            wnd.Owner = Window.GetWindow(this);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (wnd.ShowDialog() == true)
            {
                CommandsService.Run(
                    "create facet",
                    () =>
                    {
                        var newName = Features.Facets.Create(wnd.Value, selection ?? Array.Empty<string>());
                    }
                  ).Wait();
            }
        }

        private void MenuItem_Click_CreateFacet(object sender, RoutedEventArgs e)
        {
            CreateFacet();
        }

        private void CreateObject()
        {
            var wnd = new NewObject();
            wnd.Owner = Window.GetWindow(this);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (wnd.ShowDialog() == true)
            {
                CommandsService.Run(
                    "create object",
                    () => Features.Objects.Create(wnd.Value, String.Empty, wnd.Tags)
                  ).Wait();
            }
        }

        private void MenuItem_Click_CreateObject(object sender, RoutedEventArgs e)
        {
            CreateObject();
        }

        private void MenuItem_Click_CreateReference(object sender, RoutedEventArgs e)
        {
            var selection = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Name ?? String.Empty).ToArray();

            CommandsService.Run(
                "create references from selection",
                () =>
                {
                    foreach (var name in selection)
                    {
                        var newName = Features.Objects.CreateReference(name);
                    }
                }
            ).Wait();
        }

        private void DeleteSelectedObject()
        {
            var selection = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Name ?? String.Empty).ToArray();

            CommandsService.Run(
                "delete objects from selection",
                () => {
                    foreach (var name in selection)
                    {
                       Features.Objects.Delete(name).Wait();
                    }
                }
            ).Wait();
        }

        private void MenuItem_Click_DeleteObject(object sender, RoutedEventArgs e)
        {
            DeleteSelectedObject();
        }

        private void MenuItem_Click_EditOutput(object sender, RoutedEventArgs e) //todo async ?
        {
            try
            {
                var selection = (dataGrid.SelectedItem as TabItem)?.Name;

                if (selection != null)
                {
                    Features.Objects.EditContent(selection).Wait();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine(ex.Message);
            }
        }

        private void MenuItem_Click_ShowOutput(object sender, RoutedEventArgs e)
        {
            try
            {
                var selection = (dataGrid.SelectedItem as TabItem)?.Name;

                if (selection != null)
                {
                    Features.Objects.ShowContent(selection).Wait();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine(ex.Message);
            }
        }

        private void MenuItem_Click_Build(object sender, RoutedEventArgs e)
        {
            try
            {
                var selection = (dataGrid.SelectedItem as TabItem)?.Name;

                if (selection != null)
                {
                    Features.Objects.BuildTree(selection).Wait();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine(ex.Message);
            }
        }

        private void DataGrid_Drop(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                CommandsService.Run(
                    "create object from file",
                    () => Features.Objects.CreateFromFiles(files)
                ).Wait();
            }
        }

        private void AddObjectsToFacet(string facetName, Program.DevFacet facet, string[] objects)
        {
            CommandsService.Run(
                "add objects to facet",
                () =>
                {
                    try
                    {
                        DevObject._checkLock.Wait();

                        using (DevFacet.Recorder.Rec(facetName, facet))
                        {
                            foreach (var o in objects)
                            {
                                if (!facet.Objects.ContainsKey(o) && Program.DevObject.References.ContainsKey(o))
                                {
                                    if(facet.TryGetBoundingBox(out var box))
                                        facet.Objects.Add(o, new Program.DevFacet.ObjectProperties() { zone = new Rect(box.Left, box.Bottom + 5, 100, 100) });
                                    else
                                        facet.Objects.Add(o, new Program.DevFacet.ObjectProperties());
                                }
                            }
                        }

                        GuiService.InvalidateObjectsStatus(); // actualise la grille des objets
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.WriteLine(ex.Message);
                    }
                    finally
                    {
                        DevObject._checkLock.Release();
                    }
                }
            ).Wait();
        }

        private void MenuItem_AddToFacet_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var facetName = menuItem?.Header as string;
            var facet = menuItem?.Tag as Program.DevFacet;
            var objects = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Name ?? String.Empty).ToArray();

            if (facet != null)
            {
                AddObjectsToFacet(facetName, facet, objects);
            }
        }

        private void AddPointerToObject(string targetName)
        {
            var wnd = new NewPointer();
            wnd.Owner = Window.GetWindow(this);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (wnd.ShowDialog() == true)
            {
                CommandsService.Run(
                    "add pointer to object",
                    () =>
                    {
                        try
                        {
                            DevObject._checkLock.Wait();

                            var selection = dataGrid.SelectedItems.OfType<TabItem>().ToArray();
                            var selObjects = Program.DevObject.References.Where(p => selection.FirstOrDefault(pp => pp.Name == p.Key) != null).ToArray();

                            foreach (var o in selObjects)
                            {
                                using (DevObject.Recorder.Rec(o.Key, o.Value))
                                    o.Value.AddPointer(wnd.Value, targetName, []);
                            }

                            GuiService.InvalidateObjectsStatus(); // actualise la grille des objets
                        }
                        catch (Exception ex)
                        {
                            Program.Logger.WriteLine(ex.Message);
                        }
                        finally
                        {
                            DevObject._checkLock.Release();
                        }

                    }
                ).Wait();
            }
        }

        private void MenuItem_AddPointer_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var targetName = menuItem?.Header.ToString();

            if(targetName == null)
                return;

            AddPointerToObject(targetName);
        }

        private void MenuItem_ContextMenuOpening(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                menuItem.Items.Clear();
                foreach (var facet in Program.DevFacet.References)
                {
                    var item = new MenuItem();
                    item.Header = facet.Key;
                    item.Tag = facet.Value;
                    item.Click += MenuItem_AddToFacet_Click;
                    menuItem.Items.Add(item);
                }
            }
        }

        private void MenuItem_ContextMenuOpening_Pointer(object sender, RoutedEventArgs e)
        {
            var selection = dataGrid.SelectedItems.OfType<TabItem>().ToArray();
            var selObjects = Program.DevObject.References.Where(p => selection.FirstOrDefault(pp => pp.Name == p.Key) != null).Select(p => p.Value).ToArray();
            var menuItem = sender as MenuItem;
            if (menuItem != null && selection.Length > 0)
            {
                menuItem.Items.Clear();
                try
                {
                    DevObject._checkLock.Wait();

                    foreach (var obj in Program.DevObject.References)
                    {
                        var item = new MenuItem();
                        item.Header = obj.Key;
                        item.Tag = obj;
                        item.Click += MenuItem_AddPointer_Click;
                        var a = selObjects[0].Pointers.Count(p => p.Value.target == obj.Key) > 0;//cet objet est pointé par la selection ?
                        var b = obj.Value.Pointers.Count(p => p.Value.target == selection[0].Name) > 0;//cet objet pointe vers la selection ?

                        if (a && !b)
                        {
                            item.ToolTip = "Cet objet pointe déjà vers " + obj.Key;
                            item.Icon = new TextBlock() { Text = "→", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.Black };
                        }
                        if (!a && b)
                        {
                            item.ToolTip = obj.Key + " pointe déjà vers cet objet";
                            item.Icon = new TextBlock() { Text = "←", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.Black };
                        }
                        if (a && b)
                        {
                            item.ToolTip = obj.Key + " et " + selection[0].Name + " pointent déjà entre eux";
                            item.Icon = new TextBlock() { Text = "↔", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.Black };
                        }

                        menuItem.Items.Add(item);
                    }
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
        }

        private void MenuItemEditor_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var editor = menuItem?.Tag as string;
            var objects = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Name ?? String.Empty).ToArray();

            if (editor != null)
            {
                CommandsService.Run(
                    "Change object editor",
                    () =>
                    {
                        try
                        {
                            DevObject._checkLock.Wait();

                            foreach (var name in objects)
                            {
                                if (Program.DevObject.TryGet(name, out var obj))
                                {
                                    using (DevObject.Recorder.Rec(name, obj))
                                    {
                                        obj.Editor = editor;
                                    }
                                }
                            }

                            GuiService.InvalidateObjectsStatus();
                        }
                        finally
                        {
                            DevObject._checkLock.Release();
                        }
                    }
                ).Wait();
            }
        }

        private void MenuItemEditor_ContextMenuOpening(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                menuItem.Items.Clear();

                {
                    var item = new MenuItem();
                    item.Header = String.Format("Automatique");
                    item.Click += (s, e) =>
                    {
                        var objects = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Name ?? String.Empty).ToArray();
                        CommandsService.Run(
                            "Change object editor",
                            () =>
                            {
                                try
                                {
                                    DevObject._checkLock.Wait();

                                    foreach (var name in objects)
                                    {
                                        if (Program.DevObject.TryGet(name, out var obj))
                                        {
                                            using (DevObject.Recorder.Rec(name, obj))
                                            {
                                                obj.Editor = null;
                                            }
                                        }
                                    }

                                    GuiService.InvalidateObjectsStatus();
                                }
                                finally
                                {
                                    DevObject._checkLock.Release();
                                }
                            }
                        ).Wait();
                    };
                    menuItem.Items.Add(item);
                    menuItem.Items.Add(new Separator());
                }

                foreach (var editor in GuiService.associatedEditors)
                {
                    var item = new MenuItem();
                    item.Header = String.Format("{0} ⇒ {1}", editor.Key, editor.Value);
                    item.Tag = editor.Key;
                    item.Click += MenuItemEditor_Click;
                    menuItem.Items.Add(item);
                }

            }
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
                    if (text != null && item?.Name != null)
                    {
                        if (e.Column.Header.ToString() == "Nom")
                        {
                            CommandsService.Run(
                                "Rename object",
                                () => Features.Objects.Rename(item.Name, text)
                            ).Wait();
                            GuiService.InvalidateObjectsStatus();
                        }
                        else if (e.Column.Header.ToString() == "Description")
                        {
                            CommandsService.Run(
                                "Change object description",
                                () => Features.Objects.SetDescription(item.Name, text)
                            ).Wait();
                            GuiService.InvalidateObjectsStatus();
                        }
                        else if (e.Column.Header.ToString() == "Tags")
                        {
                            text = text.Replace("#", " #"); /// s'assure qu'il y a un espace devant chaque #
                            CommandsService.Run(
                                "Change object tags",
                                () => Features.Objects.SetTags(item.Name, text)
                            ).Wait();
                            GuiService.InvalidateObjectsStatus();
                        }
                    }

                    IsEditing = false;
                }
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine(ex.Message);
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
                DeleteSelectedObject();
                return;
            }
        }

        public void OnKeyState(ModifierKeys modifier)
        {
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InvalidatePointerVisual();
        }

        private void MenuItem_Click_InitialOutputObject(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(GuiService.EditorWindow, "Appliquer la valeur actuelle en tant que valeur initiale de l'objet ?", "Appliquer", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (dataGrid.SelectedItem is TabItem selectedItem && selectedItem.Name != null)
                {
                    try
                    {
                        DevObject._checkLock.Wait();

                        if (DevObject.TryGet(selectedItem.Name, out var selectedObject))
                        {
                            if (selectedObject.Content.Length == 0 && MessageBox.Show(GuiService.EditorWindow, "L'objet ne contient pas de données, voulez vous tout de même continuer ?", "Appliquer", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                                throw new Exception("Pas de données à initialiser dans l'objet " + selectedItem.Name);

                            byte[] bytes = new byte[selectedObject.Content.Length];
                            selectedObject.Content.Seek(0, SeekOrigin.Begin);
                            selectedObject.Content.Read(bytes, 0, (int)selectedObject.Content.Length);
                            selectedObject.Content.Seek(0, SeekOrigin.Begin);

                            using (DevObject.Recorder.Rec(selectedItem.Name, selectedObject))
                                selectedObject.InitialDataBase64 = Convert.ToBase64String(bytes);
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.WriteLine(ex.Message);
                    }
                    finally
                    {
                        DevObject._checkLock.Release();
                    }
                }
            }
        }

        private void MenuItem_Click_RestoreInitialOutputObject(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(GuiService.EditorWindow, "Restaurer l'état initial de l'objet ?", "Restaurer", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (dataGrid.SelectedItem is TabItem selectedItem)
                {
                    try
                    {
                        DevObject._checkLock.Wait();

                        if (DevObject.TryGet(selectedItem.Name, out var selectedObject))
                        {
                            if (selectedObject.InitialDataBase64.Length == 0 && MessageBox.Show(GuiService.EditorWindow, "Les données initiales sont vide, voulez vous tout de même continuer ?", "Restaurer", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                                throw new Exception("Pas de données à restorer dans l'objet " + selectedItem.Name);

                            var bytes = Convert.FromBase64String(selectedObject.InitialDataBase64);
                            selectedObject.Content.Seek(0, SeekOrigin.Begin);
                            selectedObject.Content.Write(bytes);
                            selectedObject.Content.SetLength(bytes.Length);
                            selectedObject.Content.Seek(0, SeekOrigin.Begin);
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.WriteLine(ex.Message);
                    }
                    finally
                    {
                        DevObject._checkLock.Release();
                    }
                }
            }
        }

        private void MenuItem_Click_CopyContent(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem is TabItem selectedItem)
            {
                try
                {
                    DevObject._checkLock.Wait();

                    if (DevObject.TryGet(selectedItem.Name, out var selectedObject))
                    {
                        byte[] bytes = new byte[selectedObject.Content.Length];
                        selectedObject.Content.Seek(0, SeekOrigin.Begin);
                        selectedObject.Content.Read(bytes, 0, (int)selectedObject.Content.Length);
                        selectedObject.Content.Seek(0, SeekOrigin.Begin);

                        Clipboard.SetText(Encoding.UTF8.GetString(bytes));
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.WriteLine(ex.Message);
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
        }

        private void MenuItem_Click_CopyBase64Content(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem is TabItem selectedItem)
            {
                try
                {
                    DevObject._checkLock.Wait();

                    if (DevObject.TryGet(selectedItem.Name, out var selectedObject))
                    {
                        byte[] bytes = new byte[selectedObject.Content.Length];
                        selectedObject.Content.Seek(0, SeekOrigin.Begin);
                        selectedObject.Content.Read(bytes, 0, (int)selectedObject.Content.Length);
                        selectedObject.Content.Seek(0, SeekOrigin.Begin);

                        Clipboard.SetText(Convert.ToBase64String(bytes));
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.WriteLine(ex.Message);
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
        }
        private void MenuItem_Click_CopyVisual(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem is TabItem selectedItem)
            {
                try
                {
                    DevObject._checkLock.Wait();

                    if (DevObject.TryGet(selectedItem.Name, out var reference))
                    {
                        // Execute le script de dessin
                        if (reference.DrawCode.Item2 != null)
                        {
                            try
                            {
                                reference._readOutput.Wait();

                                DrawingVisual visual = new DrawingVisual();

                                // Ouvre le DrawingContext
                                using (DrawingContext drawingContext = visual.RenderOpen())
                                {
                                    var engine = reference.DrawCode.Item2?.Engine;
                                    if (engine != null)
                                    {
                                        try
                                        {
                                            var pyScope = engine.CreateScope();//lock Program.pyEngine !
                                            pyScope.SetVariable("out", new DevApps.Scripts.Output(reference.Content, Path.Combine(Program.DataDir, this.Name)));
                                            pyScope.SetVariable("gui", reference.gui);
                                            pyScope.SetVariable("name", this.Name);
                                            pyScope.SetVariable("dc", drawingContext);
                                            pyScope.SetVariable("rect", new Rect(0, 0, 500, 500));
                                            pyScope.SetVariable("desc", reference.Description);

                                            foreach (var pointer in reference.Pointers)
                                            {
                                                Program.DevObject.References.TryGetValue(pointer.Value.target, out var pointerRef);
                                                pyScope.SetVariable(pointer.Key, new DevApps.Scripts.Output(pointerRef != null ? pointerRef.Content : new MemoryStream(), Path.Combine(Program.DataDir, this.Name)));// mise en cache dans l'objet ?
                                            }

                                            reference.gui.Begin(drawingContext);
                                            reference.DrawCode.Item2?.Execute(pyScope);
                                            reference.gui.End();
                                        }
                                        catch (Exception ex)
                                        {
                                            Program.Logger.WriteLine("******************************************");
                                            Program.Logger.WriteLine("OnRender: " + this.Name);
                                            Program.Logger.WriteLine(engine.FormatError(ex));
                                            Program.Logger.WriteLine("******************************************");
                                        }
                                    }
                                }

                                try
                                {
                                    var mx = new Matrix();
                                    mx.Translate(-visual.ContentBounds.X, -visual.ContentBounds.Y);
                                    visual.Transform = new MatrixTransform(mx);

                                    // Rendu du bitmap en mémoire
                                    RenderTargetBitmap bmp = new RenderTargetBitmap((int)visual.ContentBounds.Width, (int)visual.ContentBounds.Height, 96, 96, PixelFormats.Pbgra32);
                                    bmp.Render(visual);

                                    // Encode en PNG
                                    var encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(bmp));

                                    using var stream = new MemoryStream();
                                    encoder.Save(stream);
                                    stream.Position = 0;

                                    var data = new DataObject();
                                    data.SetData("PNG", stream, autoConvert: false); // format personnalisé PNG
                                    data.SetImage(bmp); // optionnel : pour compatibilité
                                    Clipboard.SetDataObject(data, true);
                                }
                                catch (Exception ex)
                                {
                                    Program.Logger.WriteLine(ex.Message);
                                }
                            }
                            finally
                            {
                                reference._readOutput.Release();
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.WriteLine(ex.Message);
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
        }

        private void MenuItem_Click_CopyBase64Visual(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem is TabItem selectedItem)
            {
                try
                {
                    DevObject._checkLock.Wait();

                    if (DevObject.TryGet(selectedItem.Name, out var reference))
                    {
                        // Execute le script de dessin
                        if (reference.DrawCode.Item2 != null)
                        {
                            try
                            {
                                reference._readOutput.Wait();

                                DrawingVisual visual = new DrawingVisual();

                                // Ouvre le DrawingContext
                                using (DrawingContext drawingContext = visual.RenderOpen())
                                {
                                    var engine = reference.DrawCode.Item2?.Engine;
                                    if (engine != null)
                                    {
                                        try
                                        {
                                            var pyScope = engine.CreateScope();//lock Program.pyEngine !
                                            pyScope.SetVariable("out", new DevApps.Scripts.Output(reference.Content, Path.Combine(Program.DataDir, this.Name)));
                                            pyScope.SetVariable("gui", reference.gui);
                                            pyScope.SetVariable("name", this.Name);
                                            pyScope.SetVariable("dc", drawingContext);
                                            pyScope.SetVariable("rect", new Rect(0, 0, 500, 500));
                                            pyScope.SetVariable("desc", reference.Description);

                                            foreach (var pointer in reference.Pointers)
                                            {
                                                Program.DevObject.References.TryGetValue(pointer.Value.target, out var pointerRef);
                                                pyScope.SetVariable(pointer.Key, new DevApps.Scripts.Output(pointerRef != null ? pointerRef.Content : new MemoryStream(), Path.Combine(Program.DataDir, this.Name)));// mise en cache dans l'objet ?
                                            }

                                            reference.gui.Begin(drawingContext);
                                            reference.DrawCode.Item2?.Execute(pyScope);
                                            reference.gui.End();
                                        }
                                        catch (Exception ex)
                                        {
                                            Program.Logger.WriteLine("******************************************");
                                            Program.Logger.WriteLine("OnRender: " + this.Name);
                                            Program.Logger.WriteLine(engine.FormatError(ex));
                                            Program.Logger.WriteLine("******************************************");
                                        }
                                    }
                                }

                                try
                                {
                                    var mx = new Matrix();
                                    mx.Translate(-visual.ContentBounds.X, -visual.ContentBounds.Y);
                                    visual.Transform = new MatrixTransform(mx);

                                    // Rendu du bitmap en mémoire
                                    RenderTargetBitmap bmp = new RenderTargetBitmap((int)visual.ContentBounds.Width, (int)visual.ContentBounds.Height, 96, 96, PixelFormats.Pbgra32);
                                    bmp.Render(visual);

                                    // Encodage PNG
                                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(bmp));

                                    // Conversion en base64
                                    using (MemoryStream stream = new MemoryStream())
                                    {
                                        encoder.Save(stream);
                                        string base64 = Convert.ToBase64String(stream.ToArray());

                                        Clipboard.SetText(base64);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Program.Logger.WriteLine(ex.Message);
                                }
                            }
                            finally
                            {
                                reference._readOutput.Release();
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.WriteLine(ex.Message);
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
        }

        private void MenuItem_Click_UpdateObjectModel(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem is TabItem selectedItem && selectedItem.Name != null && MessageBox.Show(GuiService.EditorWindow, "Voulez vous mettre à jour la bibliothèque avec le contenu de cet objet ?", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                CommandsService.Run(
                    "Update model",
                    () => Features.Objects.UpdateModel(selectedItem.Name)
                ).Wait();
                GuiService.InvalidateObjectsStatus();
            }
        }

        private void MenuItem_Click_UpdateFromObjectModel(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem is TabItem selectedItem && selectedItem.Name != null && MessageBox.Show(GuiService.EditorWindow, "Voulez écraser cet objet avec le contenu de la bibliothèque ?", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                CommandsService.Run(
                    "Update object",
                    () => Features.Objects.UpdateFromModel(selectedItem.Name)
                ).Wait();
                GuiService.InvalidateObjectsStatus();
            }
        }

        private void MenuItem_Click_Duplicate(object sender, RoutedEventArgs e)
        {
            var objects = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Name ?? String.Empty).ToArray();
            CommandsService.Run(
                "Duplicate objects",
                () => Features.Objects.Duplicates(objects)
            ).Wait();
        }
    }
}
