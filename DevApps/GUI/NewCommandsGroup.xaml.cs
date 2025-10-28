using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Program;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour NewCommandsGroup.xaml
    /// </summary>
    public partial class NewCommandsGroup : Window, INotifyPropertyChanged
    {
        public string Value { get; set; } = DevCommandGroup.GenerateName();
        public string ValidationMessage { get; set; } = String.Empty;

        private Regex Format = new Regex("^[A-z0-9_]+$");

        public NewCommandsGroup()
        {
            InitializeComponent();
            ValidationMessage = "Veuillez saisir un nom d'objet";
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
                ValidationMessage = "Veuillez saisir un nom";
            else if (Format.IsMatch(Value) == false)
                ValidationMessage = "Format invalide";
            else if (DevCommandGroup.References.ContainsKey(Value))
                ValidationMessage = "Le nom existe déjà";
            else
                ValidationMessage = String.Empty;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValidationMessage)));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            text.Focus();
        }
    }
}
