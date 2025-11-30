using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Cursework.Wpf.Converters
{
    public class IdInCollectionToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return Visibility.Collapsed;

            if (values[0] is not int id)
                return Visibility.Collapsed;

            var collection = values[1] as IEnumerable;
            if (collection == null)
                return Visibility.Collapsed;

            foreach (var item in collection)
            {
                if (item is int intItem && intItem == id)
                    return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
