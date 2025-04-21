using Microsoft.Scripting.Hosting;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using static IronPython.Modules._ast;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour ScriptEdit.xaml
    /// </summary>
    public partial class ScriptEdit : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Value { get; set; }
        public string ValidationMessage { get; set; }
        public Dictionary<string, (string, CompiledCode?)> Properties { get; set; }

        public class TabItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            internal string name;
            public string Name
            {
                get
                {
                    return name;
                }
                set
                {
                    name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
            internal string expression;
            public string? Expression
            {
                get
                {
                    return expression;
                }
                set
                {
                    expression = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
            public object? Value { get {
                    try
                    {
                        var pyScope = Program.pyEngine.CreateScope();//lock Program.pyEngine !

                        foreach (var variable in Program.DevVariable.References)
                        {
                            pyScope.SetVariable(variable.Key, variable.Value.Value);
                        }

                        foreach (var variable in Program.DevVariable.EnumPrivate())
                        {
                            pyScope.SetVariable(variable.Key, variable.Value.Value);
                        }

                        return CompiledCode?.Execute(pyScope);
                    }
                    catch (Exception ex)
                    {
                        return String.Format("ERROR: {0}", ex.Message);
                    }
            } }

            internal CompiledCode? CompiledCode;
        }

        public IEnumerable<TabItem> Items
        {
            get
            {
                return Properties.Select(p => new TabItem { name = p.Key, expression = p.Value.Item1, CompiledCode = p.Value.Item2 }).ToList();
            }
        }

        public ScriptEdit(string title, string text, Dictionary<string, (string, CompiledCode?)> properties)
        {
            InitializeComponent();
            this.DataContext = this;
            ValidationMessage = "";
            Value = text;
            textEditor.Document.Text = Value;
            Title = title;
            Properties = properties;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if(Value == textEditor.Document.Text)
                return;

            switch (MessageBox.Show("Sauvegarder les modifications ?", "Attention", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning))
            {
                case MessageBoxResult.Yes:
                    {
                        Value = textEditor.Document.Text;
                        DialogResult = true;
                    }
                    break;
                case MessageBoxResult.No:
                    {
                        DialogResult = false;
                    }
                    break;
                case MessageBoxResult.Cancel:
                    {
                        e.Cancel = true;
                    }
                    break;
            }
        }

        private void dataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var item = e.Row.DataContext as TabItem;
                var value = (e.EditingElement as TextBox)?.Text;
                if (value != null && item != null)
                {
                    try
                    {
                        ScriptSource source = Program.pyEngine.CreateScriptSourceFromString(value, Microsoft.Scripting.SourceCodeKind.Expression);
                        CompiledCode compiled = source.Compile();

                        Properties[item.Name] = (value, compiled);
                        item.CompiledCode = compiled;
                        item.Expression = value;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("L'expression est incorrecte.\n" + ex.Message, "Compilation", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        e.Cancel = true;
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ScriptSource source = Program.pyEngine.CreateScriptSourceFromString(textEditor.Document.Text, Microsoft.Scripting.SourceCodeKind.Statements);
                CompiledCode compiled = source.Compile();
                ValidationMessage = "OK";
                MessageBox.Show("Compilation OK", "Compilation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Microsoft.Scripting.SyntaxErrorException ex)
            {
                MessageBox.Show("Erreur de compilation.\n" + String.Format("L{0} C{1}: {2}", ex.Line, ex.Column, ex.Message), "Compilation", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                ValidationMessage = ex.Message;
                textEditor.Select(textEditor.Document.Lines[ex.Line - 1].Offset + ex.Column - 1, 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur de compilation.\n" + ex.Message, "Compilation", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                ValidationMessage = ex.Message;
            }

            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(ValidationMessage)));
        }
    }
}
