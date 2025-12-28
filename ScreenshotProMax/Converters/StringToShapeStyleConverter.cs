using System;
using System.Globalization;
using System.Windows.Data;
using ScreenshotProMax.Models;

namespace ScreenshotProMax.Converters;

public class StringToShapeStyleConverter : IValueConverter
{
    public static StringToShapeStyleConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ShapeStyle shapeStyle)
        {
            return shapeStyle.ToString();
        }
        return "StrokeOnly";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && Enum.TryParse<ShapeStyle>(str, out var result))
        {
            return result;
        }
        return ShapeStyle.StrokeOnly;
    }
}
