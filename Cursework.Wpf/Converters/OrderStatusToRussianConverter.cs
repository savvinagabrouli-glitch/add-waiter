using System;
using System.Globalization;
using System.Windows.Data;

namespace Cursework.Wpf.Converters
{
    public class OrderStatusToRussianConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string;

            return status switch
            {
                "Preorder" => "Предзаказ",
                "New" => "Новый",
                "Pending" => "В приготовлении",
                "ReadyToPay" => "Готов к оплате",
                "Closed" => "Закрыт",
                "Cancelled" => "Отменён",
                _ => status ?? string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
