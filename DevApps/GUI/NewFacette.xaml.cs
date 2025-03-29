using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Program;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour NewFacette.xaml
    /// </summary>
    public partial class NewFacette : Window, INotifyPropertyChanged
    {
        public string Value { get; set; }
        public string ValidationMessage { get; set; }

        private Regex Format = new Regex("^[A-z0-9_]+$");

        public NewFacette()
        {
            InitializeComponent();
            ValidationMessage = "Veuillez saisir un nom de facette";
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
                ValidationMessage = "Veuillez saisir un nom de facette";
            else if (Format.IsMatch(Value) == false)
                ValidationMessage = "Format invalide";
            else if(DevFacet.References.ContainsKey(Value))
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
