using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Controls;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour SystemConsole.xaml
    /// </summary>
    public partial class SystemConsole : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChange([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal StringBuilder _consoleText = new StringBuilder();
        public string ConsoleText
        {
            get { 
                while(ProgramLogger.Instance.ReadNext(out var line))
                {
                    _consoleText.AppendLine(line);
                }
                return _consoleText.ToString();
            }
        }

        public SystemConsole()
        {
            InitializeComponent();

            this.DataContext = this;

            _consoleText.Append(ProgramLogger.Instance.ToString());

            ProgramLogger.Instance.TextWritten += (s, e) => OnPropertyChange(nameof(ConsoleText));
        }
    }
}
