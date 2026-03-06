using System.Windows;
using System.Windows.Controls;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerFacetsView.xaml
    /// </summary>
    public partial class DesignerFacetsView : UserControl, IInvalidableView
    {
        public DesignerFacetsView()
        {
            InitializeComponent();
        }

        public void InvalidateContent()
        {
            // recharge la liste des facettes
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new NewFacette();
            wnd.Owner = Window.GetWindow(this);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (wnd.ShowDialog() == true)
            {
                Program.DevFacet.Create(wnd.Value, []);
                GuiService.EditorWindow?.InvalidateFacets();
            }
        }
    }
}
