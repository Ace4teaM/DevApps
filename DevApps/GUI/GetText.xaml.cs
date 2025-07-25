using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour GetText.xaml
    /// </summary>
    public partial class GetText : Window, INotifyPropertyChanged
    {
        public string Value { get; set; } = String.Empty;
        public string ValidationMessage { get; set; } = String.Empty;

        public bool IsMultiline { get; set; } = true;

        public Regex? Format { get; set; } = null;

        public GetText()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(IsMultiline == false && e.Key == Key.Enter)
            {
                this.Close();
            }

            e.Handled = false;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidationMessage = (Format != null && Format.IsMatch(Value) == false) ? "Format invalide" : String.Empty;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValidationMessage)));
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.DialogResult = String.IsNullOrEmpty(ValidationMessage);
        }
    }
}
