using System;
using System.Globalization;
using System.Windows.Data;

namespace Cursework.Wpf.Converters
{
    public class StatusToRussianConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string;

            return status switch
            {
                "Free" => "Свободен",
                "Occupied" => "Занят",
                "Reserved" => "Зарезервирован",
                "Cleaning" => "Уборка",
                "OutOfService" => "Не обслуживается",
                _ => status ?? string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public class ZoneToRussianConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var zone = value as string;

            return zone switch
            {
                "MainHall" => "Основной зал",
                "Terrace" => "Терраса",
                "VIP" => "VIP-зал",
                "Bar" => "Бар",
                _ => zone ?? string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
