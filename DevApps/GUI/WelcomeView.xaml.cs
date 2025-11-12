using Markdig;
using Markdig.Renderers.Wpf.Extensions;
using System.IO;
using System.Windows.Controls;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour WelcomeView.xaml
    /// </summary>
    public partial class WelcomeView : UserControl
    {
        public string Journal { get; set; }

        MarkdownPipeline? pipeline;


        public MarkdownPipeline MarkdownPipeline
        {
            get
            {
                if (pipeline == null)
                {
                    pipeline = new MarkdownPipelineBuilder()
                        .UseAdvancedExtensions()
                        .Use<Base64Extension>()
                        .Build();
                }

                return pipeline;
            }
        }

        public WelcomeView()
        {
            InitializeComponent();
            this.DataContext = this;

            try
            {
                using (StreamReader reader = new StreamReader(Program.JournalFilename))
                {
                    Journal = reader.ReadToEnd();
                }
            }
            catch
            {
                Journal = Path.GetFileName(Environment.CurrentDirectory);
            }
        }
    }
}
