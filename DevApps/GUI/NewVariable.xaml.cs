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
using static Program;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour NewVariable.xaml
    /// </summary>
    public partial class NewVariable : Window, INotifyPropertyChanged
    {
        public string Value { get; set; }
        public string ValidationMessage { get; set; }

        private Regex Format = new Regex("^[A-z0-9_]+$");

        public NewVariable()
        {
            InitializeComponent();
            ValidationMessage = "Veuillez saisir un nom de variable";
            this.DataContext = this;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && String.IsNullOrEmpty(ValidationMessage))
            {
                this.DialogResult = true;
                this.Close();
            }

            e.Handled = false;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrEmpty(Value))
                ValidationMessage = "Veuillez saisir un nom de variable";
            else if (Format.IsMatch(Value) == false)
                ValidationMessage = "Format invalide";
            else if (DevVariable.References.ContainsKey(Value))
                ValidationMessage = "Ce nom est déjà utilisé";
            else
                ValidationMessage = String.Empty;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ValidationMessage"));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            text.Focus();
        }
    }
}