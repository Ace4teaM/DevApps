using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Windows;

namespace DevApps.App
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

            try
            {
                Profile = File.ReadAllText(Path.Combine(Program.ExecutableSharedPath, "Profile.txt"));
            }
            catch (Exception ex)
            {
                Profile = String.Empty;
                Console.WriteLine(ex.Message);
            }

            try
            {
                ProfileUser = File.ReadAllText(Path.Combine(Program.ExecutableSharedPath, "Profile.user.txt"));
            }
            catch (Exception ex)
            {
                ProfileUser = String.Empty;
                Console.WriteLine(ex.Message);
            }

            try
            {
                MemoryStream stream = new MemoryStream();
                using TextWriter writer = new StreamWriter(stream);

                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                };
                JsonSerializer serializer = JsonSerializer.CreateDefault(settings);

                serializer.Serialize(writer, new Serializer.DevProject());

                writer.Flush();

                Project = Encoding.UTF8.GetString(stream.ToArray());

                stream.Dispose();
            }
            catch (Exception ex)
            {
                Project = String.Empty;
                Console.WriteLine(ex.Message);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                File.WriteAllText(Path.Combine(Program.ExecutableSharedPath, "Profile.user.txt"), ProfileUser);
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
