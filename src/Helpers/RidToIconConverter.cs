using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Dots.Helpers
{
    public class RidToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Data.Rid rid && rid.ToString() is string ridstring)
            {
                if (ridstring.Contains("Win")) return "🪟";
                else if (ridstring.Contains("Osx")) return "🍏";
                else return "🐧";
            }
            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringIsNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrEmpty(str)) return true;
            else return false;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

