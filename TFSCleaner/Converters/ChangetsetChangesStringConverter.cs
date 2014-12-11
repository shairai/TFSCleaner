using System;
using System.Linq;
using System.Windows.Data;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace SR.TFSCleaner.Converters
{
    public class ChangetsetChangesStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return null;
            string changesStr = string.Empty;
            if (value is Change[])
            {
                Change[] changes = value as Change[];
                changesStr = changes.Aggregate(changesStr, (current, change) => current + (change.ChangeType.ToString() + ", "));
            }
            return changesStr.TrimEnd(',', ' ');
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
