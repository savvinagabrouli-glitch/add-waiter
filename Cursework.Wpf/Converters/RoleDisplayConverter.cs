using System;
using System.Globalization;
using System.Windows.Data;

namespace Cursework.Wpf.Converters
{
    public class RoleDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var code = value as string;

            return code switch
            {
                "Admin" => "Администратор",
                "Waiter" => "Официант",
                _ => code
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var display = value as string;

            return display switch
            {
                "Администратор" => "Admin",
                "Официант" => "Waiter",
                _ => display
            };
        }
    }
}
