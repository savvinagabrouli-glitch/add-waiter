using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Cursework.Wpf.Converters
{
    public class SizeFromWHConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var w = values.Length > 0 && values[0] is double dw ? dw : 0;
            var h = values.Length > 1 && values[1] is double dh ? dh : 0;
            return new Size(w, h);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
