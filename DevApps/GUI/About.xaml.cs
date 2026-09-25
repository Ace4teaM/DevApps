using System.Reflection;
using System.Windows;

namespace DevApps.GUI
{
    public partial class About : Window
    {
        public string VersionText { get; }

        public string AuthorText { get; } = "Auteur : Thomas AUGUEY";

        public About()
        {
            InitializeComponent();
            DataContext = this;

            var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "N/A";
            VersionText = $"Version : {version}";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
