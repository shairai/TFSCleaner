using System;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Microsoft.TeamFoundation.Build.Client;
using SR.TFSCleaner.Models;

namespace SR.TFSCleaner.Converters
{
    public class ItemTypeToImageConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return null;
            string imageName = string.Empty;

            if (value is SourceControlItem)
            {
                SourceControlItem srcItem = (SourceControlItem)value;
                imageName = srcItem.Item.ItemType.ToString().ToLower();
                if (srcItem.Item.IsBranch)
                    imageName = "branch";
                if (srcItem.Item.DeletionId > 0)
                    imageName += "_deleted";

            }
            else if (value is BuildDetail)
            {
                BuildDetail build = (BuildDetail)value;
                imageName = build.Build.Status.ToString();
            }

            return new BitmapImage(new Uri("/TFSCleaner;component/Images/" + imageName + ".png", UriKind.RelativeOrAbsolute));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
