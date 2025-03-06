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
    /// Logique d'interaction pour GetText.xaml
    /// </summary>
    public partial class GetText : Window, INotifyPropertyChanged
    {
        public string Value { get; set; }
        public string ValidationMessage { get; set; }

        public bool IsMultiline { get; set; }

        public Regex? Format { get; set; }

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
                this.DialogResult = true;
                this.Close();
            }

            e.Handled = false;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidationMessage = (Format != null && Format.IsMatch(Value) == false) ? "Format invalide" : "";

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ValidationMessage"));
        }
    }
}
