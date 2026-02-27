using Microsoft.Scripting.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Program;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerCommandsView.xaml
    /// </summary>
    public partial class DesignerCommandsView : UserControl, INotifyPropertyChanged, IKeyCommand
    {
        public event PropertyChangedEventHandler? PropertyChanged;


        public class TabItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            public TabItem(string Name)
            {
                this.name = Name;

                if (String.IsNullOrEmpty(Name) || Program.DevCommandGroup.References.ContainsKey(Name) == false)
                    throw new ArgumentException(nameof(Name));

                this.label = Program.DevCommandGroup.References[Name].Label;
                this.output = Program.DevCommandGroup.References[Name].Output;
                this.commands = Program.DevCommandGroup.References[Name].ToString();
            }

            public TabItem()
            {
                this.name = DevCommandGroup.GenerateName();

                Program.DevCommandGroup.References[Name] = new DevCommandGroup();

                this.label = String.Empty;
                this.output = String.Empty;
                this.commands = String.Empty;
            }

            internal string name;
            public string Name
            {
                get
                {
                    return name;
                }
                set
                {
                    if (String.IsNullOrEmpty(value))
                        return;

                    if (String.IsNullOrEmpty(name))
                    {
                        Program.DevCommandGroup.References.Add(value, DevCommandGroup.FromString("Nouvelle commande","",commands));
                        name = value;
                    }
                    else
                    {
                        // renomme la clé
                        var existing = Program.DevCommandGroup.References[name];
                        Program.DevCommandGroup.References.Remove(name);
                        Program.DevCommandGroup.References.Add(value, existing);
                        name = value;
                    }
                }
            }
            internal string label;
            public string Label
            {
                get
                {
                    return label;
                }
                set
                {
                    label = value;
                    Program.DevCommandGroup.References[Name].Label = label;
                }
            }
            internal string output;
            public string Output
            {
                get
                {
                    return output;
                }
                set
                {
                    output = value;
                    Program.DevCommandGroup.References[Name].Output = output;
                }
            }
            internal string commands;
            public string Commands
            {
                get
                {
                    return commands;
                }
                set
                {
                    commands = value;
                    Program.DevCommandGroup.References[Name].Content = commands;
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
            items.AddRange(Program.DevCommandGroup.References.Select(p => new TabItem(p.Key)));

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
        }

        public DesignerCommandsView()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Loaded += DesignerDataView_Loaded;
        }

        private void DesignerDataView_Loaded(object sender, RoutedEventArgs e)
        {
            InvalidateItems();
        }

        private void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var context = dataGrid.SelectedItem as TabItem;

            if (context == null)
                return;

            if(Program.DevCommandGroup.References.TryGetValue(context.Name, out var group))
            {
                try
                {
                    var wnd = new CommandsEdit();
                    wnd.Value = group.Content;
                    wnd.Label = group.Label;
                    wnd.Output = group.Output;
                    wnd.Owner = Window.GetWindow(this);
                    wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    if (wnd.ShowDialog() == true)
                    {
                        var cmd = DevCommandGroup.FromString(wnd.Label, wnd.Output, wnd.Value);
                        cmd.MakeCommands();
                        Program.DevCommandGroup.References[context.Name] = cmd;
                        InvalidateItems();
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.WriteLine(ex.Message);
                }
            }
        }

        private void CreateCommandGroup()
        {
            var wnd = new NewCommandsGroup();
            wnd.Owner = Window.GetWindow(this);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (wnd.ShowDialog() == true)
            {
                Program.DevCommandGroup.Create(wnd.Value, String.Empty);
                InvalidateItems();
            }
        }

        private void MenuItem_Click_CreateCommandGroup(object sender, RoutedEventArgs e)
        {
            CreateCommandGroup();
        }

        private void DeleteCommandGroup()
        {
            var selection = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Name ?? String.Empty).ToArray();

            foreach (var name in selection)
            {
                if (Program.DevCommandGroup.References.ContainsKey(name))
                {
                    Program.DevCommandGroup.Delete(name);
                }
            }

            InvalidateItems();
        }

        private void MenuItem_Click_DeleteCommandGroup(object sender, RoutedEventArgs e)
        {
            DeleteCommandGroup();
        }

        private void ExecuteCommandGroup()
        {
            var selection = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Name ?? String.Empty).ToArray();

            foreach (var name in selection)
            {
                if (Program.DevCommandGroup.References.ContainsKey(name))
                {
                    Program.DevCommandGroup.References[name].Execute();
                }
                else
                {
                    Program.Logger.WriteLine($"Command group '{name}' not found.");
                }
            }
        }

        private void MenuItem_Click_ExecuteCommandGroup(object sender, RoutedEventArgs e)
        {
            ExecuteCommandGroup();
        }

        public void OnKeyCommand(KeyCommand command)
        {
            if (command == KeyCommand.Create)
            {
                CreateCommandGroup();
                return;
            }
            if (command == KeyCommand.Delete)
            {
                DeleteCommandGroup();
                return;
            }
        }

        public void OnKeyState(ModifierKeys modifier)
        {
        }

        private void MenuItem_Click_AddToFacet(object sender, RoutedEventArgs e)
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

        private void MenuItem_AddFacet_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var facet = menuItem?.Tag as Program.DevFacet;
            var items = dataGrid.SelectedItems.OfType<TabItem>().Select(p => p.Name ?? String.Empty).ToArray();

            if (facet != null)
            {
                foreach (var o in items)
                {
                    if (!facet.Commands.ContainsKey(o) && Program.DevCommandGroup.References.ContainsKey(o))
                        facet.Commands.Add(o, new Program.DevFacet.CommandProperties());
                }
            }
        }
    }
}
