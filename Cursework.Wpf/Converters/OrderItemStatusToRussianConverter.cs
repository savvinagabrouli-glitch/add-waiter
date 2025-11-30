using System;
using System.Globalization;
using System.Windows.Data;

namespace Cursework.Wpf.Converters
{
    public class OrderItemStatusToRussianConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string;

            return status switch
            {
                "Ordered" => "Заказано",
                "Preparing" => "Готовится",
                "Served" => "Подано",
                "Cancelled" or "Canceled" => "Отменено",
                _ => status ?? string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
