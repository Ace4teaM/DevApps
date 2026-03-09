using DevApps.Commands;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static IronPython.Modules._ast;
using static Program;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerVariablesView.xaml
    /// </summary>
    public partial class DesignerVariablesView : UserControl, INotifyPropertyChanged, IKeyCommand, IInvalidableView
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// true Si une cellule est en cours d'édition
        /// </summary>
        private bool IsEditing = false;

        public class TabItem
        {
            public string Name { get; set; } = String.Empty;
            public string? Description { get; set; }
            public object? Value
            {
                get
                {
                    var obj = Program.DevVariable.References.FirstOrDefault(p => p.Key == Name);
                    return obj.Value.Value;
                }
                set
                {
                    var obj = Program.DevVariable.References.FirstOrDefault(p => p.Key == Name);
                    obj.Value.Value = DevVariable.Variant.Parse(value?.ToString());
                }
            }
        }

        public class TabPrivateItem
        {
            public string Name { get; set; } = String.Empty;
            public string? Description { get; set; }
            public object Value
            {
                get
                {
                    var obj = Program.DevVariable.GetPrivate(Name);
                    return obj;
                }
                set
                {
                    Program.DevVariable.SetPrivate(Name, value);
                }
            }
        }

        public IEnumerable<TabItem> Items
        {
            get
            {
                try
                {
                    Program.DevVariable._checkLock.Wait();

                    return Program.DevVariable.References.Select(p => new TabItem { Name = p.Key, Description = p.Value.Description }).ToList();
                }
                finally
                {
                    Program.DevVariable._checkLock.Release();
                }
            }
        }

        public IEnumerable<TabPrivateItem> PrivateItems
        {
            get
            {
                try
                {
                    Program.DevVariable._checkLock.Wait();
                    return Program.DevVariable.EnumPrivate().Select(p => new TabPrivateItem { Name = p.Key, Description = p.Value.Description }).ToList();
                }
                finally
                {
                    Program.DevVariable._checkLock.Release();
                }
            }
        }

        public void InvalidateContent()
        {
            InvalidateVariables();
            InvalidatePrivateVariables();
        }

        internal void InvalidateVariables()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
        }

        internal void InvalidatePrivateVariables()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PrivateItems)));
        }

        public void InvalidateVariablesStatus()// todo a optimiser
        {
            InvalidateVariables();
        }

        public DesignerVariablesView()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            IsEditing = true;
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var item = e.Row.DataContext as TabItem;
                var text = (e.EditingElement as TextBox)?.Text;
                if (text != null && item != null)
                {
                    if (e.Column.Header.ToString() == "Nom")
                    {
                        if (text != item.Name)
                        {
                            CommandsService.Run(
                                "Rename variable",
                                Features.Variables.Rename(item.Name, text)
                                ).Wait();
                        }
                    }
                    else if (e.Column.Header.ToString() == "Description")
                    {
                        CommandsService.Run(
                            "Change variable description",
                            Features.Variables.SetDescription(item.Name, text)
                            ).Wait();
                    }
                    else if (e.Column.Header.ToString() == "Valeur")
                    {
                        CommandsService.Run(
                            "Change variable value",
                            Features.Variables.SetValue(item.Name, text)
                            ).Wait();
                    }
                }

                IsEditing = e.Cancel == true;
            }
        }

        private void DataGrid_CellEditEnding2(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var item = e.Row.DataContext as TabPrivateItem;
                var text = (e.EditingElement as TextBox)?.Text;
                if (text != null && item != null)
                {
                    try
                    {
                        Program.DevVariable._checkLock.Wait();

                        var value = Program.DevVariable.LoadPrivate(item.Name, out var reference);

                        if (reference != null)
                        {
                            if (e.Column.Header.ToString() == "Nom")
                            {
                                if (text != item.Name)
                                {
                                    // NOTE inutile d'historiser cette action car elle n'a pas vocation a être appelé à distance
                                    Program.DevVariable.SavePrivate(text, reference, item.Name);

                                    // renomme l'objet dans les references des autres objets
                                    try
                                    {
                                        DevObject._checkLock.Wait();

                                        foreach (var obj in Program.DevObject.References)
                                        {
                                            foreach (var property in obj.Value.Properties.Where(p => p.Value.Item1.Contains(item.Name)).ToArray())
                                            {
                                                property.Value.Item1.Replace(item.Name, text); // todo rechercher dans la syntaxe et non seulement le texte !
                                                Program.Logger.WriteLine($"Renomme dans la propriété {obj.Key}.{property.Key} => {property.Value.Item1}");
                                                //todo recompiler l'expression...
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        DevObject._checkLock.Release();
                                    }


                                    // renomme l'objet
                                    item.Name = text;//sans effet

                                    InvalidatePrivateVariables();
                                }
                            }
                            else if (e.Column.Header.ToString() == "Description")
                            {
                                item.Description = text;
                                reference.Description = text;
                                Program.DevVariable.SavePrivate(item.Name, reference);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.WriteLine(ex.Message);
                    }
                    finally
                    {
                        Program.DevVariable._checkLock.Release();
                    }
                }

                IsEditing = e.Cancel == true;
            }
        }

        private void CreatePublicVariable()
        {
            var wnd = new NewVariable();
            wnd.Owner = Window.GetWindow(this);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (wnd.ShowDialog() == true)
            {
                CommandsService.Run(
                    "Create variable",
                    Features.Variables.Create(wnd.Value, String.Empty)
                    ).Wait();
            }
        }

        private void CreatePrivateVariable()
        {
            var wnd = new NewVariable();
            wnd.Owner = Window.GetWindow(this);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (wnd.ShowDialog() == true)
            {
                // NOTE inutile d'historiser cette action car elle n'a pas vocation a être appelé à distance
                Program.DevVariable.SavePrivate(wnd.Value, new Program.DevVariable(), null);
                InvalidatePrivateVariables();
            }
        }

        private void CreateVariable()
        {
            if ((Keyboard.FocusedElement is FrameworkElement e))
            {
                // Variable publique
                if (e.DataContext is TabItem)
                {
                    CreatePublicVariable();
                }
                // Variable privée
                else if (e.DataContext is TabPrivateItem)
                {
                    CreatePrivateVariable();
                }
            }
        }

        private void DeletePublicVariable()
        {
            var count = dataGrid.SelectedItems.Count;
            if (count == 0)
                return;
            if (MessageBox.Show(GuiService.EditorWindow, count > 1 ? $"Voulez-vous supprimer ces {count} variables ?" : $"Voulez-vous supprimer cette variable ?", "Supprimer", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                CommandsService.Run(
                    "Delete variable",
                    () =>
                    {
                        try
                        {
                            Program.DevVariable._checkLock.Wait();

                            foreach (var item in dataGrid.SelectedItems.OfType<TabItem>())
                            {
                                if(DevVariable.TryGet(item.Name, out var obj))
                                {
                                    using (DevVariable.Recorder.Rem(item.Name, obj))
                                        Program.DevVariable.References.Remove(item.Name);
                                }
                                else
                                    throw new Exception($"La variable {item.Name} n'existe pas");
                            }
                        }
                        finally
                        {
                            Program.DevVariable._checkLock.Release();
                        }

                        DevApps.GUI.GuiService.InvalidateVariables();
                    }
                    ).Wait();

            }
        }

        private void DeletePrivateVariable()
        {
            var count = dataGrid2.SelectedItems.Count;
            if (count == 0)
                return;
            if (MessageBox.Show(GuiService.EditorWindow, count > 1 ? $"Voulez-vous supprimer ces {count} variables ?" : $"Voulez-vous supprimer cette variable ?", "Supprimer", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (var item in dataGrid2.SelectedItems.OfType<TabPrivateItem>())
                {
                    // NOTE inutile d'historiser cette action car elle n'a pas vocation a être appelé à distance
                    Program.DevVariable.DeletePrivate(item.Name);
                }
                InvalidatePrivateVariables();
            }
        }

        private void DeleteVariable()
        {
            if ((Keyboard.FocusedElement is FrameworkElement e))
            {
                // Variable publique
                if (e.DataContext is TabItem)
                {
                    DeletePublicVariable();
                }
                // Variable privée
                else if (e.DataContext is TabPrivateItem)
                {
                    DeletePrivateVariable();
                }
            }
        }

        private void MenuItem_Click_CreateVariable(object sender, RoutedEventArgs e)
        {
            CreatePublicVariable();
        }

        private void MenuItem_Click_CreateVariable2(object sender, RoutedEventArgs e)
        {
            CreatePrivateVariable();
        }

        private void MenuItem_Click_DeleteVariable(object sender, RoutedEventArgs e)
        {
            DeletePublicVariable();
        }

        private void MenuItem_Click_DeleteVariable2(object sender, RoutedEventArgs e)
        {
            DeletePrivateVariable();
        }

        public void OnKeyCommand(KeyCommand command)
        {
            if (IsEditing == true)
                return;

            if (command == KeyCommand.Create)
            {
                CreateVariable();
                return;
            }
            if (command == KeyCommand.Delete)
            {
                DeleteVariable();
                return;
            }
        }

        public void OnKeyState(ModifierKeys modifier)
        {
        }
    }
}
