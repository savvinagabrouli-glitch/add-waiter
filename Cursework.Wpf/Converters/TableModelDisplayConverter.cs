using System;
using System.Globalization;
using System.Windows.Data;

namespace Cursework.Wpf.Converters
{
    public class TableModelDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value?.ToString();
            return s switch
            {
                "Single" => "Одноместный",
                "Multi" => "Многоместный",
                _ => s ?? ""
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value?.ToString();
            // если вдруг начнут приходить “Одинарный/Многоместный”
            return s switch
            {
                "Одноместный" => "Single",
                "Многоместный" => "Multi",
                _ => s ?? "Single"
            };
        }
    }
}
