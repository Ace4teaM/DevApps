using Microsoft.Extensions.Primitives;
using System.Globalization;
using System.Windows.Data;

namespace DevApps.GUI
{
    public class StringToEditCharConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return String.Empty;
            }

            if (value.GetType() == typeof(string))
                return ((string)value).Length == 0 ? String.Empty : "✎";

            return String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (value?.ToString()) == "✎";
    }
}
