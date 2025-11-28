using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using ScreenshotProMax.Models;

namespace ScreenshotProMax.Converters;

public class ResizeHandlesToVisibilityConverter : IValueConverter
{
    public static ResizeHandlesToVisibilityConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ObservableCollection<ResizeHandleInfo> handles)
        {
            return handles.Count > 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
        return System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}