using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour ScriptEdit.xaml
    /// </summary>
    public partial class ScriptEdit : Window
    {
        public string Value { get; set; }
        public string ValidationMessage { get; set; }

        public ScriptEdit(string text)
        {
            InitializeComponent();
            this.DataContext = this;
            ValidationMessage = "";
            Value = text;
            textEditor.Document.Text = Value;
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
    }
}
