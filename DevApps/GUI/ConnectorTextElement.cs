using System.Windows.Controls;
using System.Windows.Media;

namespace DevApps.GUI
{
    internal class ConnectorTextElement : TextBlock
    {
        public static FontFamily Font = new FontFamily("Verdana");

        public ConnectorTextElement(ConnectorElement connector,  string text)
        {
            this.Text = text;
            this.Tag = connector;
            this.Foreground = Brushes.Gray;
            this.Background = Brushes.White;
            this.FontSize = 14;
            this.FontFamily = Font;
        }
    }
}
