using Serializer;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using static IronPython.Modules._ast;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerVariablesView.xaml
    /// </summary>
    public partial class DesignerVariablesView : UserControl, INotifyPropertyChanged, IKeyCommand
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public class TabItem
        {
            public string Name { get; set; }
            public string? Description { get; set; }
            public object Value
            {
                get
                {
                    var obj = Program.DevVariable.References.FirstOrDefault(p => p.Key == Name);
                    return obj.Value.Value;
                }
                set
                {
                    var obj = Program.DevVariable.References.FirstOrDefault(p => p.Key == Name);
                    obj.Value.Value = value;
                }
            }
        }

        public class TabPrivateItem
        {
            public string Name { get; set; }
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
                Program.DevVariable.mutexCheckVariableList.WaitOne();
                var list = Program.DevVariable.References.Select(p => new TabItem { Name = p.Key, Description = p.Value.Description }).ToList();
                Program.DevVariable.mutexCheckVariableList.ReleaseMutex();
                return list;
            }
        }

        public IEnumerable<TabPrivateItem> PrivateItems
        {
            get
            {
                Program.DevVariable.mutexCheckVariableList.WaitOne();
                var list = Program.DevVariable.EnumPrivate().Select(p => new TabPrivateItem { Name = p.Key, Description = p.Value.Description }).ToList();
                Program.DevVariable.mutexCheckVariableList.ReleaseMutex();
                return list;
            }
        }

        internal void InvalidateVariables()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Items"));
        }

        internal void InvalidatePrivateVariables()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PrivateItems"));
        }

        public DesignerVariablesView()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void dataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var item = e.Row.DataContext as TabItem;
                var text = (e.EditingElement as TextBox)?.Text;
                if (text != null && item != null)
                {
                    Program.DevVariable.mutexCheckVariableList.WaitOne();
                    try
                    {
                        Program.DevVariable.References.TryGetValue(item.Name, out var reference);

                        if (reference != null)
                        {
                            if (e.Column.Header.ToString() == "Nom")
                            {
                                if (text != item.Name)
                                {
                                    Program.DevVariable.MakeUniqueName(ref text);
                                    var value = Program.DevVariable.References[item.Name];
                                    Program.DevVariable.References.Remove(item.Name);
                                    Program.DevVariable.References[text] = value;

                                    // renomme l'objet dans les references des autres objets
                                    Program.DevObject.mutexCheckObjectList.WaitOne();
                                    foreach (var obj in Program.DevObject.References)
                                    {
                                        foreach (var property in obj.Value.Properties.Where(p => p.Value.Item1.Contains(item.Name)).ToArray())
                                        {
                                            property.Value.Item1.Replace(item.Name, text); // todo rechercher dans la syntaxe et non seulement le texte !
                                            Console.WriteLine($"Renomme dans la propriété {obj.Key}.{property.Key} => {property.Value.Item1}");
                                            //todo recompiler l'expression...
                                        }
                                    }
                                    Program.DevObject.mutexCheckObjectList.ReleaseMutex();

                                    // renomme l'objet
                                    item.Name = text;//sans effet

                                    InvalidateVariables();
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
                        Program.DevVariable.mutexCheckVariableList.ReleaseMutex();
                    }
                }
            }
        }

        private void dataGrid_CellEditEnding2(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var item = e.Row.DataContext as TabPrivateItem;
                var text = (e.EditingElement as TextBox)?.Text;
                if (text != null && item != null)
                {
                    Program.DevVariable.mutexCheckVariableList.WaitOne();
                    try
                    {
                        var value = Program.DevVariable.LoadPrivate(item.Name, out var reference);

                        if (reference != null)
                        {
                            if (e.Column.Header.ToString() == "Nom")
                            {
                                if (text != item.Name)
                                {
                                    Program.DevVariable.SavePrivate(text, reference, item.Name);

                                    // renomme l'objet dans les references des autres objets
                                    Program.DevObject.mutexCheckObjectList.WaitOne();
                                    foreach (var obj in Program.DevObject.References)
                                    {
                                        foreach (var property in obj.Value.Properties.Where(p => p.Value.Item1.Contains(item.Name)).ToArray())
                                        {
                                            property.Value.Item1.Replace(item.Name, text); // todo rechercher dans la syntaxe et non seulement le texte !
                                            Console.WriteLine($"Renomme dans la propriété {obj.Key}.{property.Key} => {property.Value.Item1}");
                                            //todo recompiler l'expression...
                                        }
                                    }
                                    Program.DevObject.mutexCheckObjectList.ReleaseMutex();

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
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        Program.DevVariable.mutexCheckVariableList.ReleaseMutex();
                    }
                }
            }
        }

        private void CreatePublicVariable()
        {
            var wnd = new NewVariable();
            wnd.Owner = Window.GetWindow(this);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (wnd.ShowDialog() == true)
            {
                Program.DevVariable.Create(wnd.Value, String.Empty);
                InvalidateVariables();
            }
        }

        private void CreatePrivateVariable()
        {
            var wnd = new NewVariable();
            wnd.Owner = Window.GetWindow(this);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (wnd.ShowDialog() == true)
            {
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
            if (MessageBox.Show(count > 1 ? $"Voulez-vous supprimer ces {count} variables ?" : $"Voulez-vous supprimer cette variable ?", "Supprimer", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (var item in dataGrid.SelectedItems.OfType<TabItem>())
                {
                    Program.DevVariable.Delete(item.Name);
                }
                InvalidateVariables();
            }
        }

        private void DeletePrivateVariable()
        {
            var count = dataGrid2.SelectedItems.Count;
            if (count == 0)
                return;
            if (MessageBox.Show(count > 1 ? $"Voulez-vous supprimer ces {count} variables ?" : $"Voulez-vous supprimer cette variable ?", "Supprimer", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (var item in dataGrid2.SelectedItems.OfType<TabPrivateItem>())
                {
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
