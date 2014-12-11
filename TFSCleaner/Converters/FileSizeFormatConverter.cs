using System;
using System.Windows.Data;
using SR.TFSCleaner.Helpers;

namespace SR.TFSCleaner.Converters
{
    public class FileSizeFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "0";
            return string.Format(new FileSizeFormatProvider(), "{0:fs}", value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
