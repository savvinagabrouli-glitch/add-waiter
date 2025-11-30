using System;
using System.Globalization;
using System.Windows.Data;

namespace Cursework.Wpf.Converters
{
    public class IdFromCountConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return "";

            int id = 0;
            int maxId = 0;

            if (values[0] is int i)
                id = i;

            if (values[1] is int tc)
                maxId = tc;

            if (id == 0)
                return (maxId + 1).ToString();

            return id.ToString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}
