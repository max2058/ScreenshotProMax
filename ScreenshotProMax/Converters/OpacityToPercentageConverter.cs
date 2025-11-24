using System;
using System.Globalization;
using System.Windows.Data;

namespace ScreenshotProMax.Converters;

public class OpacityToPercentageConverter : IValueConverter
{
    public static OpacityToPercentageConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double opacity)
        {
            return Math.Round(opacity * 100);
        }
        return 100;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percentage)
        {
            return percentage / 100.0;
        }
        return 1.0;
    }
}
