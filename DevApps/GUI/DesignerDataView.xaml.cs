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
using static System.Windows.Forms.DataFormats;

namespace DevApps.GUI
{
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
            public string? Name { get; set; }
            public string? Description { get; set; }
            public string? UserAction { get; set; }
            public string? LoopMethod { get; set; }
            public string? InitMethod { get; set; }
            public string? BuildMethod { get; set; }
            public string? DrawCode { get; set; }
        }

        public IEnumerable<TabItem> Items
        {
            get
            {
                Program.DevObject.mutexCheckObjectList.WaitOne();
                var list = Program.DevObject.References.Select(p => new TabItem { Name = p.Key, Description = p.Value.Description, UserAction = p.Value.UserAction.Item1, LoopMethod = p.Value.LoopMethod.Item1, InitMethod = p.Value.InitMethod.Item1, BuildMethod = p.Value.BuildMethod.Item1, DrawCode = p.Value.DrawCode.Item1 }).ToList();
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
            var content = item?.Tag.ToString();
            var context = item?.DataContext as TabItem;

            Program.DevObject.mutexCheckObjectList.WaitOne();
            try
            {
                var obj = Program.DevObject.References.First(p => p.Key == context.Name).Value;

                var wnd = new ScriptEdit(content);
                wnd.Owner = Window.GetWindow(this);
                wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (wnd.ShowDialog() == true)
                {
                    switch(item.Name)
                    {
                        case "DrawCode":
                            obj.SetDrawCode(wnd.Value);
                            context.DrawCode = wnd.Value;
                            break;
                        case "BuildMethod":
                            obj.SetBuildMethod(wnd.Value);
                            context.BuildMethod = wnd.Value;
                            break;
                        case "LoopMethod":
                            obj.SetLoopMethod(wnd.Value);
                            context.LoopMethod = wnd.Value;
                            break;
                        case "InitMethod":
                            obj.SetInitMethod(wnd.Value);
                            context.InitMethod = wnd.Value;
                            break;
                        case "UserAction":
                            obj.SetUserAction(wnd.Value);
                            context.UserAction = wnd.Value;
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

                        var wnd = new DevApps.GUI.GetText();
                        wnd.Value = Encoding.UTF8.GetString(reference.buildStream.GetBuffer());//reference.GetOutput()
                        wnd.IsMultiline = true;
                        wnd.Owner = Window.GetWindow(this);
                        wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                        if (wnd.ShowDialog() == true)
                        {
                            reference.SetOutput(wnd.Value);
                        }

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
    }
}
