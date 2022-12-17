using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace PowderDetector.Converters
{
    [ValueConversion(typeof(string), typeof(Visibility))]
    public class PathVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
                return File.Exists(stringValue) ? Visibility.Collapsed : Visibility.Visible;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
