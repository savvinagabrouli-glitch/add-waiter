using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Cursework.Wpf.Converters
{
    public class TabHeaderToSubtitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TabItem tab)
                value = tab.Header;

            if (value is string header && !string.IsNullOrWhiteSpace(header))
                return $"Раздел: {header}";

            return "Панель администратора";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
