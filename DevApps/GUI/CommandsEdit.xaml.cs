using System.ComponentModel;
using System.Text;
using System.Windows;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour CommandsEdit.xaml
    /// </summary>
    public partial class CommandsEdit : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string AvailableCommands
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var cmd in Program.DevCommandDefinition.BuiltIn)
                {
                    sb.AppendLine(cmd.Key + " - " + cmd.Value.Description);
                }
                return sb.ToString();
            }
        }

        public string Value { get; set; } = string.Empty;

        public CommandsEdit()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DialogResult != null)
                return;

            switch (MessageBox.Show("Sauvegarder les modifications ?", "Attention", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning))
            {
                case MessageBoxResult.Yes:
                    {
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
