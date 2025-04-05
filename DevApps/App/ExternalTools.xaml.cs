using DevApps.GUI;
using Microsoft.Scripting.Utils;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace DevApps.App
{
    /// <summary>
    /// Logique d'interaction pour ExternalTools.xaml
    /// </summary>
    public partial class ExternalTools : Window
    {
        public class KeyValuePair
        {
            public string Key { get; set; }
            public string Value { get; set; }

            public KeyValuePair()
            {
                Key = "key";
                Value = "path";
            }

            public KeyValuePair(string key, string value)
            {
                Key = key;
                Value = value;
            }
        }
        public class ObservableCollectionKeyValue : ObservableCollection<KeyValuePair>
        {
            public ObservableCollectionKeyValue(Dictionary<string, string> values)
            {
                this.AddRange(values.Select(x => new KeyValuePair(x.Key, x.Value)));
            }

            public void AddRange(Dictionary<string, string> values)
            {
                this.AddRange(values.Select(x => new KeyValuePair(x.Key, x.Value)));
            }
        }

        public ObservableCollectionKeyValue AppsList { get; set; } = new ObservableCollectionKeyValue(Service.externalsTools);

        public ExternalTools()
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
            foreach (var item in AppsList)
            {
                Service.externalsTools[item.Key] = item.Value;
            }

            Service.SaveTools();

            DialogResult = true;
        }

        KeyValuePair? editedKeyValuePair;
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;
            var selected = e.AddedItems[0] as KeyValuePair;
            editedKeyValuePair.Value = selected.Key;
        }

        private void KeyValueGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            editedKeyValuePair = e.Row.DataContext as KeyValuePair;
        }
    }
}
