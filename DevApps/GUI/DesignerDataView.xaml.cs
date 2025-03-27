using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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
    }
}
