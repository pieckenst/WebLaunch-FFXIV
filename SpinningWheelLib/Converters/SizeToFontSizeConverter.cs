using System;
using System.Globalization;
using System.Windows.Data;
namespace SpinningWheelLib.Converters
{
    public class SizeToFontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double size)
            {
                return size * 0.2; // 20% of the control size
            }
            return 12.0;
        }



        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double fontSize)
            {
                return fontSize / 0.2; // Reverse the conversion by dividing by 0.2
            }
            return 60.0; // Default value (12.0 / 0.2)
        }


    }
}
    