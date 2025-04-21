using ICSharpCode.AvalonEdit.Editing;
using Microsoft.Scripting.Utils;
using Serializer;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using static IronPython.Modules._ast;
using static Program;
using static System.Windows.Forms.DataFormats;

namespace DevApps.GUI
{
    public class BoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return String.Empty;
            }

            if (value.GetType() == typeof(bool))
                return ((bool)value) == true ? "✗" : String.Empty;

            return String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class EditConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return String.Empty;
            }

            if (value.GetType() == typeof(string))
                return (value as string).Length == 0 ? String.Empty : "✎";

            return String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>
    /// Logique d'interaction pour DesignerDataView.xaml
    /// </summary>
    public partial class DesignerDataView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public class TabItem
        {
            public bool? IsReference { 
                get
                {
                    var obj = Program.DevObject.References.FirstOrDefault(p => p.Key == Name).Value;
                    return obj?.IsReference;
                } 
            }
            public string? Name { get; set; }
            public string? Description { get; set; }
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
        }

        public IEnumerable<TabItem> Items
        {
            get
            {
                Program.DevObject.mutexCheckObjectList.WaitOne();
                var list = Program.DevObject.References.Select(p => new TabItem { Name = p.Key, Description = p.Value.Description }).ToList();
                Program.DevObject.mutexCheckObjectList.ReleaseMutex();
                return list;
            }
        }

        internal void InvalidateObjects()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Items"));
        }

        public DesignerDataView()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((sender as ContentControl)?.Content as FrameworkElement);
            var content = String.Empty;
            var context = ((sender as ContentControl)?.DataContext as TabItem);

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

            Program.DevObject.mutexCheckObjectList.WaitOne();
            try
            {
                var obj = Program.DevObject.References.First(p => p.Key == context.Name).Value;

                var wnd = new ScriptEdit(String.Format("{0} ({1})", context.Name, item.Name), content, obj.Properties);
                wnd.Owner = Window.GetWindow(this);
                wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (wnd.ShowDialog() == true)
                {
                    switch(item.Name)
                    {
                        case "DrawCode":
                            obj.SetDrawCode(wnd.Value);
                            break;
                        case "BuildMethod":
                            obj.SetBuildMethod(wnd.Value);
                            break;
                        case "LoopMethod":
                            obj.SetLoopMethod(wnd.Value);
                            break;
                        case "InitMethod":
                            obj.SetInitMethod(wnd.Value);
                            break;
                        case "UserAction":
                            obj.SetUserAction(wnd.Value);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Program.DevObject.mutexCheckObjectList.ReleaseMutex();
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
                Service.InvalidateFacets();
            }
        }

        private void MenuItem_Click_CreateObject(object sender, RoutedEventArgs e)
        {
            var wnd = new NewObject();
            wnd.Owner = Window.GetWindow(this);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (wnd.ShowDialog() == true)
            {
                Program.DevObject.Create(wnd.Value, String.Empty);
                InvalidateObjects();
            }
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

                    if (obj.IsReference)
                       Program.DevObject.CreateReference(newName, (obj as Program.DevObjectReference).BaseObjectName);
                    else
                        Program.DevObject.CreateReference(newName, name);
                }
            }

            InvalidateObjects();
        }

        private void MenuItem_Click_EditOutput(object sender, RoutedEventArgs e)
        {
            try
            {
                var selection = (dataGrid.SelectedItem as TabItem)?.Name;

                if (selection != null)
                {
                    Program.DevObject.mutexCheckObjectList.WaitOne();
                    Program.DevObject.References.TryGetValue(selection, out var reference);
                    Program.DevObject.mutexCheckObjectList.ReleaseMutex();

                    if (reference != null)
                    {
                        reference.mutexReadOutput.WaitOne();

                        Service.OpenEditorOrDefault(reference.buildStream, reference.Editor);

                        reference.mutexReadOutput.ReleaseMutex();

                        //DrawObject.InvalidateVisual();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void MenuItem_Click_Build(object sender, RoutedEventArgs e)
        {
            try
            {
                var selection = (dataGrid.SelectedItem as TabItem)?.Name;

                if (selection != null)
                {
                    Program.DevObject.mutexCheckObjectList.WaitOne();
                    var items = Program.DevObject.References.Where(p=>p.Key == selection);
                    Program.DevObject.mutexCheckObjectList.ReleaseMutex();

                    if (items != null)
                    {
                        Program.DevObject.Build(items);

                        //DrawObject.InvalidateVisual();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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
                            objects.Add(o);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                if(objects.Count > 0)
                {
                    Program.DevObject.MakeReferences(objects);
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

                foreach (var editor in Service.associatedEditors)
                {
                    var item = new MenuItem();
                    item.Header = String.Format("{0} ⇒ {1}", editor.Key, editor.Value);
                    item.Tag = editor.Key;
                    item.Click += MenuItemEditor_Click;
                    menuItem.Items.Add(item);
                }

            }
        }

        private void dataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var item = e.Row.DataContext as TabItem;
                var text = (e.EditingElement as TextBox)?.Text;
                if (text != null && item != null)
                {
                    Program.DevObject.mutexCheckObjectList.WaitOne();
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
                                        if(obj.Value is Program.DevObjectReference)
                                        {
                                            var objRef = obj.Value as Program.DevObjectReference;
                                            if(objRef.baseObjectName == item.Name)
                                            {
                                                objRef.baseObjectName = text;
                                                Console.WriteLine($"Renomme Reference {obj.Key} : {item.Name} => {text}");
                                            }
                                        }
                                    }

                                    // renomme l'objet dans les references des autres objets
                                    foreach (var obj in Program.DevObject.References)
                                    {
                                        foreach(var pointer in obj.Value.Pointers.Where(p=>p.Value == item.Name).ToArray())
                                        {
                                            obj.Value.Pointers.Remove(pointer.Key);
                                            obj.Value.Pointers.Add(pointer.Key, text);
                                            Console.WriteLine($"Renomme {pointer.Key} : {item.Name} => {text}");
                                        }
                                    }

                                    // renomme l'objet dans les references des facettes
                                    foreach(var obj in Program.DevFacet.References)
                                    {
                                        foreach(var pointer in obj.Value.Objects.Where(p=>p.Key == item.Name).ToArray())
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
