using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Program;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour NewObject.xaml
    /// </summary>
    public partial class NewObject : Window, INotifyPropertyChanged
    {
        public string[] Tags { get { return tagList.Children.OfType<Tag>().Select(p => p.Content.ToString()).ToArray(); } }
        public string Value { get; set; }
        public string ValidationMessage { get; set; }

        internal Regex Format = new Regex("^[A-z0-9_]+$");

        private List<string> suggestions;

        public NewObject()
        {
            InitializeComponent();
            ValidationMessage = "Veuillez saisir un nom d'objet";
            this.DataContext = this;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Liste de suggestions
            suggestions = new List<string>();
            suggestions.AddRange(TagService.UsageTags);
            suggestions.AddRange(TagService.LangagesTags);
            suggestions.AddRange(TagService.TypeTags);
            suggestions.AddRange(TagService.FormatTags);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && String.IsNullOrEmpty(ValidationMessage))
            {
                this.DialogResult = true;
                this.Close();
            }

            e.Handled = false;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrEmpty(Value))
                ValidationMessage = "Veuillez saisir un nom d'objet";
            else if (Format.IsMatch(Value) == false)
                ValidationMessage = "Format invalide";
            else if (DevObject.References.ContainsKey(Value))
                ValidationMessage = "Ce nom est déjà utilisé";
            else
                ValidationMessage = String.Empty;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ValidationMessage"));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            text.Focus();
        }

        private bool TagExists(string tag)
        {
            return tagList.Children.OfType<Tag>().Count(p=>p.Content.ToString().ToLower() == tag.ToLower()) > 0;
        }

        private void TagBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = tag.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                suggestionsPopup.IsOpen = false;
                return;
            }

            var filteredSuggestions = suggestions.Where(s => s.StartsWith(text, System.StringComparison.OrdinalIgnoreCase)).ToList();
            suggestionsListBox.ItemsSource = filteredSuggestions;
            suggestionsPopup.IsOpen = filteredSuggestions.Any();
        }

        private void TagBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab && suggestionsListBox.Items.Count > 0)
            {
                var tagText = "#" + suggestionsListBox.Items[0].ToString();
                if (TagExists(tagText) == false)
                {
                    tagList.Children.Add(new Tag { Content = tagText });
                    tag.Text = String.Empty;
                    tag.CaretIndex = 0;
                    suggestionsPopup.IsOpen = false;
                }
                e.Handled = true;
            }
            if (e.Key == Key.Enter && string.IsNullOrWhiteSpace(tag.Text) == false)
            {
                var tagText = tag.Text.Trim();
                if (tagText.StartsWith("#") == false)
                    tagText = "#" + tagText;
                if (TagService.TagFormat.IsMatch(tagText) == true && TagExists(tagText) == false)
                {
                    tagList.Children.Add(new Tag { Content = tagText });
                    tag.Text = String.Empty;
                    tag.CaretIndex = 0;
                    suggestionsPopup.IsOpen = false;
                }
                e.Handled = true;
            }
        }

        private void SuggestionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suggestionsListBox.SelectedItem != null)
            {
                var tagText = suggestionsListBox.SelectedItem.ToString();
                if (tagText.StartsWith("#") == false)
                    tagText = "#" + tagText;
                if (TagService.TagFormat.IsMatch(tagText) == true && TagExists(tagText) == false)
                {
                    tagList.Children.Add(new Tag { Content = tagText });
                    tag.Text = String.Empty;
                    tag.CaretIndex = 0;
                    suggestionsPopup.IsOpen = false;
                }
                e.Handled = true;
            }
        }

        private bool TryGetParentOfType<T>(FrameworkElement? e, out T? parent) where T : class
        {
            while(e != null)
            {
                if (e is T)
                {
                    parent = e as T;
                    return true;
                }
                e = e.TemplatedParent as FrameworkElement;
            }
            parent = null;
            return false;
        }

        private void tagList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (TryGetParentOfType<Tag>(e.MouseDevice.DirectlyOver as FrameworkElement, out var tag))
            {
                tagList.Children.Remove(tag);
            }
        }
    }
}
