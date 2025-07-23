using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Windows;

namespace DevApps.Appli
{
    /// <summary>
    /// Logique d'interaction pour AiProfile.xaml
    /// </summary>
    public partial class AiProfile : Window
    {
        public string Profile { get; set; }
        public string ProfileUser { get; set; }
        public string Project { get; set; }
        public AiProfile()
        {
            InitializeComponent();
            this.DataContext = this;

            Profile = AI.Profile.GetContext();
            Project = AI.Profile.GetProject();

            try
            {
                ProfileUser = File.ReadAllText(Path.Combine(Program.ExecutablePath, Program.IaUserProfile));
            }
            catch (Exception ex)
            {
                ProfileUser = String.Empty;
                Console.WriteLine(ex.Message);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                File.WriteAllText(Path.Combine(Program.ExecutablePath, "Profile.user.txt"), ProfileUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(Project);
        }

        private void MenuItem2_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(Profile);
        }
    }
}
