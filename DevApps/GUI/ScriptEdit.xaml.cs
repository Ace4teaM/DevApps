using DevApps.Scripts;
using ICSharpCode.AvalonEdit.Highlighting;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DevApps.GUI
{
    public class SyntaxNameToDefinitionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string syntaxName = value as string;
            if (string.IsNullOrEmpty(syntaxName))
                return null;

            return HighlightingManager.Instance.GetDefinition(syntaxName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var def = value as IHighlightingDefinition;
            return def?.Name;
        }
    }

    /// <summary>
    /// Logique d'interaction pour ScriptEdit.xaml
    /// </summary>
    public partial class ScriptEdit : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Infos { get; set; } = String.Empty;
        public string Value { get; set; } = String.Empty;
        public string ValidationMessage { get; set; } = String.Empty;
        public Dictionary<string, (string, CompiledCode?)> Properties { get; set; }

        public ScriptEngine CurrentEngine;

        public string SyntaxName { get; set; }

        public bool IsPython
        {
            get
            {
                return CurrentEngine == Program.pythonEngine;
            }
        }

        public bool IsJavascript
        {
            get
            {
                return CurrentEngine == Program.javascriptEngine;
            }
        }

        internal void ToggleJavascriptLanguage()
        {
            CurrentEngine = Program.javascriptEngine;
            SyntaxName = CurrentEngine.HighlightName;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SyntaxName)));
            textEditor.TextArea.TextView.Redraw();
        }

        internal void TogglePythonLanguage()
        {
            CurrentEngine = Program.pythonEngine;
            SyntaxName = CurrentEngine.HighlightName;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SyntaxName)));
            textEditor.TextArea.TextView.Redraw();
        }

        public class TabItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            internal ScriptEdit editor;

            internal string name = String.Empty;
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
            internal string? expression;
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
                        var pyScope = editor.CurrentEngine.CreateScope();//lock Program.pyEngine !

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
                return Properties.Select(p => new TabItem { editor = this, name = p.Key, expression = p.Value.Item1, CompiledCode = p.Value.Item2 }).ToList();
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
            CurrentEngine = Program.GetScriptEngine(text);
            SyntaxName = CurrentEngine.HighlightName;
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
                        ScriptSource source = CurrentEngine.CreateExpressionFromString(value);
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
                ScriptSource source = CurrentEngine.CreateStatementsFromString(textEditor.Document.Text);
                CompiledCode compiled = source.Compile();
                ValidationMessage = "OK";
                MessageBox.Show("Compilation OK", "Compilation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Microsoft.Scripting.SyntaxErrorException ex)
            {
                MessageBox.Show("Erreur de compilation.\n" + String.Format("L{0} C{1}: {2}", ex.Line, ex.Column, ex.Message), "Compilation", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                ValidationMessage = ex.Message;
                try
                {
                    textEditor.Select(textEditor.Document.Lines[ex.Line - 1].Offset + ex.Column - 1, 1);
                }
                catch{}
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur de compilation.\n" + ex.Message, "Compilation", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                ValidationMessage = ex.Message;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValidationMessage)));
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            Value = textEditor.Document.Text;
            DialogResult = true;
        }

        private void RadioButton_Python_Click(object sender, RoutedEventArgs e)
        {
            TogglePythonLanguage();
        }

        private void RadioButton_Javascript_Click(object sender, RoutedEventArgs e)
        {
            ToggleJavascriptLanguage();
        }
    }
}
