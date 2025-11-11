using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Program;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerDataView.xaml
    /// </summary>
    public partial class DesignerDataView : UserControl, INotifyPropertyChanged, IKeyCommand
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public class TabItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            public void OnPropertyChanged(string name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
                    return String.Join(',', facettes);
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

        internal void InvalidateObjects()
        {
            var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
            if (handle)
            {
                items.Clear();
                items.AddRange(new ObservableCollection<TabItem>(Program.DevObject.References.Select(p => new TabItem { Name = p.Key, Description = p.Value.Description, Tags = String.Join(' ', p.Value.Tags) })));
                Program.DevObject.mutexCheckObjectList.ReleaseMutex();
            }
            
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
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

            if (item == null || context == null)
                return;

            switch (item.Name)
            {
                case "DrawCode":
                    content = context.DrawCode;
                    break;
                case "BuildMethod":
                    content = context.BuildMethod;
                    break;
                case "LoopMethod":
                    content = context.LoopMethod;
                    break;
                case "InitMethod":
                    content = context.InitMethod;
                    break;
                case "UserAction":
                    content = context.UserAction;
                    break;
            }

            DevObject? obj = null;

            var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
            if (handle)
            {
                obj = Program.DevObject.References.First(p => p.Key == context.Name).Value;
                Program.DevObject.mutexCheckObjectList.ReleaseMutex();
            }

            if (obj != null)
            {
                try
                {
                    var wnd = new ScriptEdit(String.Format("{0} ({1})", context.Name, item.Name), content ?? string.Empty, obj.Properties);

                    // Infos
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine(String.Format("{0} = {1}", "out", "output"));
                    stringBuilder.AppendLine(String.Format("{0} = {1}", "name", "nom de l'objet"));
                    stringBuilder.AppendLine(String.Format("{0} = {1}", "desc", "description de l'objet"));

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
                    wnd.Infos = stringBuilder.ToString();


                    wnd.Owner = Window.GetWindow(this);
                    wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    if (wnd.ShowDialog() == true)
                    {
                        try
                        {
                            switch (item.Name)
                            {
                                case "DrawCode":
                                    obj.SetDrawCode(wnd.Value);
                                    obj.CompilDraw();
                                    break;
                                case "BuildMethod":
                                    obj.SetBuildMethod(wnd.Value);
                                    obj.CompilBuild();
                                    break;
                                case "LoopMethod":
                                    obj.SetLoopMethod(wnd.Value);
                                    obj.CompilLoop();
                                    break;
                                case "InitMethod":
                                    obj.SetInitMethod(wnd.Value);
                                    obj.CompilInit();
                                    break;
                                case "UserAction":
                                    obj.SetUserAction(wnd.Value);
                                    obj.CompilUserAction();
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Erreur de compilation. " + ex.Message, "Compilation", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void MenuItem_Click_CreateFacet(object sender, RoutedEventArgs e)
        {
            var selection = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Name ?? String.Empty).ToArray();

            var wnd = new NewFacette();
            wnd.Owner = Window.GetWindow(this);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (wnd.ShowDialog() == true)
            {
                Program.DevFacet.Create(wnd.Value, selection ?? Array.Empty<string>());
                GuiService.InvalidateFacets();
            }
        }

        private void CreateObject()
        {
            var wnd = new NewObject();
            wnd.Owner = Window.GetWindow(this);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (wnd.ShowDialog() == true)
            {
                Program.DevObject.Create(wnd.Value, String.Empty, wnd.Tags);
                InvalidateObjects();
            }
        }

        private void MenuItem_Click_CreateObject(object sender, RoutedEventArgs e)
        {
            CreateObject();
        }

        private void MenuItem_Click_CreateReference(object sender, RoutedEventArgs e)
        {
            var selection = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Name ?? String.Empty).ToArray();

            foreach(var name in selection)
            {
                if (Program.DevObject.References.ContainsKey(name))
                {
                    var obj = Program.DevObject.References[name];
                    var newName = name + "Ref";
                    Program.DevObject.MakeUniqueName(ref newName);

                    if (obj is Program.DevObjectReference reference)
                    {
                        Assert.NotNull(reference.BaseObjectName);
                        #pragma warning disable CS8604
                        Program.DevObject.CreateReference(newName, reference.BaseObjectName);
                    }
                    else
                        Program.DevObject.CreateReference(newName, name);
                }
            }

            InvalidateObjects();
        }
        private void DeleteObject()
        {
            var selection = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Name ?? String.Empty).ToArray();

            foreach (var name in selection)
            {
                if (Program.DevObject.References.ContainsKey(name))
                {
                    Program.DevObject.DeleteObject(name);
                }
            }

            InvalidateObjects();
        }

        private void MenuItem_Click_DeleteObject(object sender, RoutedEventArgs e)
        {
            DeleteObject();
        }

        private void MenuItem_Click_EditOutput(object sender, RoutedEventArgs e)
        {
            try
            {
                var selection = (dataGrid.SelectedItem as TabItem)?.Name;

                if (selection != null)
                {
                    DevObject? reference = null;

                    var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
                    if (handle)
                    {
                        Program.DevObject.References.TryGetValue(selection, out reference);
                        Program.DevObject.mutexCheckObjectList.ReleaseMutex();
                    }

                    if (reference != null)
                    {
                        var handle2 = reference.mutexReadOutput.WaitOne();
                        if (handle2)
                        {
                            if(GuiService.OpenEditorOrDefault(reference.Content, reference.Editor, true))
                            {
                                DevObject.IncrementBuilt(selection);

                                // Actualise les compteurs
                                foreach (var i in Items)
                                {
                                    i.OnPropertyChanged(nameof(i.BuildIndex));
                                    i.OnPropertyChanged(nameof(i.MustBeBuild));
                                }
                            }

                            reference.mutexReadOutput.ReleaseMutex();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void MenuItem_Click_ShowOutput(object sender, RoutedEventArgs e)
        {
            try
            {
                var selection = (dataGrid.SelectedItem as TabItem)?.Name;

                if (selection != null)
                {
                    DevObject? reference = null;

                    var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
                    if (handle)
                    {
                        Program.DevObject.References.TryGetValue(selection, out reference);
                        Program.DevObject.mutexCheckObjectList.ReleaseMutex();
                    }

                    if (reference != null)
                    {
                        var handle2 = reference.mutexReadOutput.WaitOne();
                        if (handle2)
                        {
                            GuiService.OpenEditorOrDefault(reference.Content, reference.Editor, false);

                            reference.mutexReadOutput.ReleaseMutex();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void InvalidateObjectsStatus()
        {
            // Actualise les compteurs
            foreach (var i in Items)
            {
                i.OnPropertyChanged(nameof(i.BuildIndex));
                i.OnPropertyChanged(nameof(i.MustBeBuild));
            }
        }

        private void MenuItem_Click_Build(object sender, RoutedEventArgs e)
        {
            try
            {
                var selection = (dataGrid.SelectedItem as TabItem)?.Name;

                if (selection != null)
                {
                    var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
                    if (handle)
                    {
                        var items = Program.DevObject.References.Where(p => p.Key == selection);

                        Program.DevObject.mutexCheckObjectList.ReleaseMutex();

                        if (items != null)
                        {
                            foreach(var item in items)
                            {
                                Program.DevObject.BuildTree(item);
                            }

                            // Actualise les compteurs
                            InvalidateObjectsStatus();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
                            objects.Add(o);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                if(objects.Count > 0)
                {
                    Program.DevObject.CompilObjects(objects);
                    Program.DevObject.Init();

                    InvalidateObjects();
                }
            }
        }

        private void MenuItem_AddFacet_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var facet = menuItem?.Tag as Program.DevFacet;
            var objects = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Name ?? String.Empty).ToArray();

            if (facet != null)
            {
                foreach(var o in objects)
                {
                    if(!facet.Objects.ContainsKey(o) && Program.DevObject.References.ContainsKey(o))
                        facet.Objects.Add(o, new Program.DevFacet.ObjectProperties());
                }
            }
        }

        private void MenuItem_AddPointer_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var targetObject = menuItem?.Tag as Program.DevObject;
            var targetName = menuItem?.Header.ToString();

            var wnd = new NewPointer();
            wnd.Owner = Window.GetWindow(this);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (wnd.ShowDialog() == true)
            {
                var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
                if (handle)
                {
                    var selection = dataGrid.SelectedItems.OfType<TabItem>().ToArray();
                    var selObjects = Program.DevObject.References.Where(p => selection.FirstOrDefault(pp => pp.Name == p.Key) != null).Select(p => p.Value).ToArray();

                    foreach (var o in selObjects)
                    {
                        o.AddPointer(wnd.Value, targetName, []);
                    }

                    Program.DevObject.mutexCheckObjectList.ReleaseMutex();
                }
            }
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
                    item.Click += MenuItem_AddFacet_Click;
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
                var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
                if (handle)
                {
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
                    Program.DevObject.mutexCheckObjectList.ReleaseMutex();
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
                foreach(var o in objects)
                {
                    if(Program.DevObject.References.ContainsKey(o))
                        Program.DevObject.References[o].Editor = editor;
                }
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
                        foreach (var o in objects)
                        {
                            if (Program.DevObject.References.ContainsKey(o))
                                Program.DevObject.References[o].Editor = null;
                        }
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
                    if (text != null && item != null)
                    {
                        var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
                        if (handle)
                        {
                            try
                            {
                                Program.DevObject.References.TryGetValue(item.Name, out var reference);

                                if (reference != null)
                                {
                                    if (e.Column.Header.ToString() == "Nom")
                                    {
                                        if (text != item.Name)
                                        {
                                            Program.DevObject.MakeUniqueName(ref text);
                                            var value = Program.DevObject.References[item.Name];
                                            Program.DevObject.References.Remove(item.Name);
                                            Program.DevObject.References[text] = value;

                                            // renomme l'objet dans les objets de references
                                            foreach (var obj in Program.DevObject.References)
                                            {
                                                if (obj.Value is Program.DevObjectReference objRef)
                                                {
                                                    if (objRef.baseObjectName == item.Name)
                                                    {
                                                        objRef.baseObjectName = text;
                                                        Console.WriteLine($"Renomme Reference {obj.Key} : {item.Name} => {text}");
                                                    }
                                                }
                                            }

                                            // renomme l'objet dans les references des autres objets
                                            foreach (var obj in Program.DevObject.References)
                                            {
                                                foreach (var pointer in obj.Value.Pointers.Where(p => p.Value.target == item.Name).ToArray())
                                                {
                                                    obj.Value.Pointers[pointer.Key].target = text;
                                                    Console.WriteLine($"Renomme {pointer.Key} : {item.Name} => {text}");
                                                }
                                            }

                                            // renomme l'objet dans les references des facettes
                                            foreach (var obj in Program.DevFacet.References)
                                            {
                                                foreach (var pointer in obj.Value.Objects.Where(p => p.Key == item.Name).ToArray())
                                                {
                                                    var tmp = pointer.Value;
                                                    obj.Value.Objects.Remove(pointer.Key);
                                                    obj.Value.Objects.Add(text, tmp);
                                                    Console.WriteLine($"Renomme {obj.Key} : {pointer.Key} => {text}");
                                                }
                                            }

                                            // renomme l'objet
                                            item.Name = text;//sans effet

                                            InvalidateObjects();
                                        }
                                    }
                                    else if (e.Column.Header.ToString() == "Description")
                                    {
                                        item.Description = text;
                                        reference.Description = text;
                                    }
                                    else if (e.Column.Header.ToString() == "Tags")
                                    {
                                        text = text.Replace("#", " #"); /// s'assure qu'il y a un espace devant chaque #
                                        var tags = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(p => TagService.TagFormat.IsMatch(p)).ToArray();
                                        var instance = reference.IsReference ? ((Program.DevObjectReference)reference).baseObject : (Program.DevObjectInstance)reference;
                                        instance!.tags = new HashSet<string>(tags);
                                        item.Tags = String.Join(' ', instance.tags);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                            finally
                            {
                                Program.DevObject.mutexCheckObjectList.ReleaseMutex();
                            }
                        }
                    }

                    IsEditing = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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

        public void OnKeyState(ModifierKeys modifier)
        {
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataGrid.SelectedItem is TabItem selectedItem)
            {
                var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
                if (handle)
                {
                    try
                    {
                        if (DevObject.References.TryGetValue(selectedItem.Name, out var selectedObject))
                        {
                            selectedItem.IsPointed = false;
                            selectedItem.IsPointer = false;
                            foreach (var item in Items)
                            {
                                if (item != selectedItem && DevObject.References.TryGetValue(item.Name, out var obj))
                                {
                                    item.IsPointed = selectedObject.Pointers.Count(p => p.Value.target == item.Name) > 0;//cet objet est pointé par la selection ?
                                    item.IsPointer = obj.Pointers.Count(p => p.Value.target == selectedItem.Name) > 0;//cet objet pointe vers la selection ?
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        Program.DevObject.mutexCheckObjectList.ReleaseMutex();
                    }
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

        private void MenuItem_Click_InitialOutputObject(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Appliquer la valeur actuelle en tant que valeur initiale de l'objet ?", "Appliquer", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (dataGrid.SelectedItem is TabItem selectedItem)
                {
                    var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
                    if (handle)
                    {
                        try
                        {
                            if (DevObject.References.TryGetValue(selectedItem.Name, out var selectedObject))
                            {
                                if (selectedObject.Content.Length == 0 && MessageBox.Show("L'objet ne contient pas de données, voulez vous tout de même continuer ?", "Appliquer", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                                    throw new Exception("Pas de données à initialiser dans l'objet " + selectedItem.Name);

                                byte[] bytes = new byte[selectedObject.Content.Length];
                                selectedObject.Content.Seek(0, SeekOrigin.Begin);
                                selectedObject.Content.Read(bytes, 0, (int)selectedObject.Content.Length);
                                selectedObject.Content.Seek(0, SeekOrigin.Begin);

                                selectedObject.InitialDataBase64 = Convert.ToBase64String(bytes);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            Program.DevObject.mutexCheckObjectList.ReleaseMutex();
                        }
                    }
                }
            }
        }

        private void MenuItem_Click_RestoreInitialOutputObject(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Restaurer l'état initial de l'objet ?", "Restaurer", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (dataGrid.SelectedItem is TabItem selectedItem)
                {
                    var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
                    if (handle)
                    {
                        try
                        {
                            if (DevObject.References.TryGetValue(selectedItem.Name, out var selectedObject))
                            {
                                if (selectedObject.InitialDataBase64.Length == 0 && MessageBox.Show("Les données initiales sont vide, voulez vous tout de même continuer ?", "Restaurer", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
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
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            Program.DevObject.mutexCheckObjectList.ReleaseMutex();
                        }
                    }
                }
            }
        }

        private void MenuItem_Click_CopyContent(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem is TabItem selectedItem)
            {
                var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
                if (handle)
                {
                    try
                    {
                        if (DevObject.References.TryGetValue(selectedItem.Name, out var selectedObject))
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
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        Program.DevObject.mutexCheckObjectList.ReleaseMutex();
                    }
                }
            }
        }

        private void MenuItem_Click_CopyBase64Content(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem is TabItem selectedItem)
            {
                var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
                if (handle)
                {
                    try
                    {
                        if (DevObject.References.TryGetValue(selectedItem.Name, out var selectedObject))
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
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        Program.DevObject.mutexCheckObjectList.ReleaseMutex();
                    }
                }
            }
        }
        private void MenuItem_Click_CopyVisual(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem is TabItem selectedItem)
            {
                var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
                if (handle)
                {
                    try
                    {
                        if (DevObject.References.TryGetValue(selectedItem.Name, out var reference))
                        {
                            // Execute le script de dessin
                            if (reference.DrawCode.Item2 != null)
                            {
                                var handle2 = reference.mutexReadOutput.WaitOne();
                                if (handle2)
                                {
                                    DrawingVisual visual = new DrawingVisual();

                                    // Ouvre le DrawingContext
                                    using (DrawingContext drawingContext = visual.RenderOpen())
                                    {
                                        try
                                        {
                                            var pyScope = Program.pyEngine.CreateScope();//lock Program.pyEngine !
                                            pyScope.SetVariable("out", new DevApps.PythonExtends.Output(reference.Content, Path.Combine(Program.DataDir, this.Name)));
                                            pyScope.SetVariable("gui", reference.gui);
                                            pyScope.SetVariable("name", this.Name);
                                            pyScope.SetVariable("dc", drawingContext);
                                            pyScope.SetVariable("rect", new Rect(0, 0, 500, 500));
                                            pyScope.SetVariable("desc", reference.Description);

                                            foreach (var pointer in reference.Pointers)
                                            {
                                                Program.DevObject.References.TryGetValue(pointer.Value.target, out var pointerRef);
                                                pyScope.SetVariable(pointer.Key, new DevApps.PythonExtends.Output(pointerRef != null ? pointerRef.Content : new MemoryStream(), Path.Combine(Program.DataDir, this.Name)));// mise en cache dans l'objet ?
                                            }

                                            reference.gui.Begin(drawingContext);
                                            reference.DrawCode.Item2?.Execute(pyScope);
                                            reference.gui.End();
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Console.WriteLine("******************************************");
                                            System.Console.WriteLine("OnRender: " + this.Name);
                                            ExceptionOperations eo = Program.pyEngine.GetService<ExceptionOperations>();
                                            string error = eo.FormatException(ex);
                                            System.Console.WriteLine(error);
                                            System.Console.WriteLine("******************************************");
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
                                        System.Console.WriteLine(ex.Message);
                                    }

                                    reference.mutexReadOutput.ReleaseMutex();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        Program.DevObject.mutexCheckObjectList.ReleaseMutex();
                    }
                }
            }
        }

        private void MenuItem_Click_CopyBase64Visual(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem is TabItem selectedItem)
            {
                var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
                if (handle)
                {
                    try
                    {
                        if (DevObject.References.TryGetValue(selectedItem.Name, out var reference))
                        {
                            // Execute le script de dessin
                            if (reference.DrawCode.Item2 != null)
                            {
                                var handle2 = reference.mutexReadOutput.WaitOne();
                                if (handle2)
                                {
                                    DrawingVisual visual = new DrawingVisual();

                                    // Ouvre le DrawingContext
                                    using (DrawingContext drawingContext = visual.RenderOpen())
                                    {
                                        try
                                        {
                                            var pyScope = Program.pyEngine.CreateScope();//lock Program.pyEngine !
                                            pyScope.SetVariable("out", new DevApps.PythonExtends.Output(reference.Content, Path.Combine(Program.DataDir, this.Name)));
                                            pyScope.SetVariable("gui", reference.gui);
                                            pyScope.SetVariable("name", this.Name);
                                            pyScope.SetVariable("dc", drawingContext);
                                            pyScope.SetVariable("rect", new Rect(0, 0, 500, 500));
                                            pyScope.SetVariable("desc", reference.Description);

                                            foreach (var pointer in reference.Pointers)
                                            {
                                                Program.DevObject.References.TryGetValue(pointer.Value.target, out var pointerRef);
                                                pyScope.SetVariable(pointer.Key, new DevApps.PythonExtends.Output(pointerRef != null ? pointerRef.Content : new MemoryStream(), Path.Combine(Program.DataDir, this.Name)));// mise en cache dans l'objet ?
                                            }

                                            reference.gui.Begin(drawingContext);
                                            reference.DrawCode.Item2?.Execute(pyScope);
                                            reference.gui.End();
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Console.WriteLine("******************************************");
                                            System.Console.WriteLine("OnRender: " + this.Name);
                                            ExceptionOperations eo = Program.pyEngine.GetService<ExceptionOperations>();
                                            string error = eo.FormatException(ex);
                                            System.Console.WriteLine(error);
                                            System.Console.WriteLine("******************************************");
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
                                        System.Console.WriteLine(ex.Message);
                                    }

                                    reference.mutexReadOutput.ReleaseMutex();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        Program.DevObject.mutexCheckObjectList.ReleaseMutex();
                    }
                }
            }
        }
    }
}
