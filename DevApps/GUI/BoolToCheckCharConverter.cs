using System.Globalization;
using System.Windows.Data;

namespace DevApps.GUI
{
    public class BoolToCheckCharConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return String.Empty;
            }

            if (value.GetType() == typeof(bool))
                return ((bool)value) == true ? "✗" : String.Empty;

            return String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (value?.ToString()) == "✗";
    }
}
