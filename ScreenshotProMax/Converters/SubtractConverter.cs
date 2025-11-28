using System;
using System.Globalization;
using System.Windows.Data;

namespace ScreenshotProMax.Converters;

/// <summary>
/// Konvertiert einen Wert durch Subtraktion eines festen Betrags (für Offset-Berechnungen)
/// </summary>
public class SubtractConverter : IValueConverter
{
    public static SubtractConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double doubleValue && parameter is string paramStr && double.TryParse(paramStr, out double offset))
        {
            return doubleValue - offset;
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
