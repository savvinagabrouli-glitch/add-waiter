using System;
using System.Globalization;
using System.Windows.Data;

namespace Cursework.Wpf.Converters
{
    public class CallWaiterTypeToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var code = value as string;

            return code switch
            {
                "Call" => "Вызов",
                "AcceptPreorder" => "Принять предзаказ",
                _ => code ?? string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;

            return text switch
            {
                "Вызов" => "Call",
                "Принять предзаказ" => "AcceptPreorder",
                _ => text ?? string.Empty
            };
        }
    }
}
