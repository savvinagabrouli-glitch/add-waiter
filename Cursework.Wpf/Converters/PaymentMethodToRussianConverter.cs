using System;
using System.Globalization;
using System.Windows.Data;

namespace Cursework.Wpf.Converters

{
    public class PaymentMethodToRussianConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var method = value as string;

            return method switch
            {
                "Cash" => "Наличные",
                "Card" => "Карта",
                "QR" => "QR-код",
                "Other" => "Другое",
                _ => method ?? string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
