using DevApps.GUI;
using Microsoft.Scripting.Utils;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static IronPython.Modules.PythonIterTools;

namespace DevApps.Appli
{
    /// <summary>
    /// Logique d'interaction pour ExternalEditors.xaml
    /// </summary>
    public partial class ExternalEditors : Window
    {
        public class KeyValuePair
        {
            public string Key { get; set; }
            public string Value { get; set; }

            public KeyValuePair()
            {
                Key = "key";
                Value = "value";
            }

            public KeyValuePair(string key, string value)
            {
                Key = key;
                Value = value;
            }
        }
        public class ObservableCollectionKeyValue  : ObservableCollection<KeyValuePair>
        {
            public ObservableCollectionKeyValue(Dictionary<string,string> values)
            {
                this.AddRange(values.Select(x => new KeyValuePair(x.Key, x.Value)));
            }

            public void AddRange(Dictionary<string, string> values)
            {
                this.AddRange(values.Select(x => new KeyValuePair(x.Key, x.Value)));
            }
        }

        public ObservableCollectionKeyValue KeysList { get; set; } = new ObservableCollectionKeyValue(GuiService.associatedEditors);
        public ObservableCollectionKeyValue AppsList { get; set; } = new ObservableCollectionKeyValue(GuiService.externalsEditors);

        public ExternalEditors()
        {
            InitializeComponent();

            this.DataContext = this;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            GuiService.associatedEditors.Clear();

            foreach (var item in KeysList)
            {
                GuiService.associatedEditors[item.Key] = item.Value;
            }

            GuiService.externalsEditors.Clear();

            foreach (var item in AppsList)
            {
                GuiService.externalsEditors[item.Key] = item.Value;
            }

            GuiService.SaveEditors();

            DialogResult = true;
        }

        KeyValuePair? editedKeyValuePair;
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0 || editedKeyValuePair == null)
                return;
            var selected = e.AddedItems[0] as KeyValuePair;
            if(selected == null) return;
            editedKeyValuePair.Value = selected.Key;
        }

        private void KeyValueGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            editedKeyValuePair = e.Row.DataContext as KeyValuePair;
        }

        private void AppSearch_Click(object sender, RoutedEventArgs e)
        {
            var found = new Dictionary<string, string>();
            var search = new string[] { appName.Text };
            GuiService.ResolveApplicationNames(search, found);

            int count = 0;
            foreach (var app in found)
            {
                if (AppsList.Count(p => p.Key.Contains(app.Key)) == 0)
                {
                    AppsList.Add(new KeyValuePair(app.Key, app.Value));
                    count++;
                }
            }

            if (count == 0)
            {
                MessageBox.Show("Aucune nouvelle application trouvée");
            }
            else
            {
                editorGrid.SelectedIndex = AppsList.Count-1;
                MessageBox.Show(count + " nouvelle(s) application(s) trouvée(s)");
            }
        }
    }
}
