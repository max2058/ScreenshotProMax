using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ScreenshotProMax.Models;

public enum AnnotationType
{
    None,
    Number,
    Text,
    Line,
    Arrow,
    Pen
}

public partial class AnnotationModel : ObservableObject
{
    [ObservableProperty]
    private AnnotationType type;

    [ObservableProperty]
    private ObservableCollection<Point> points = new();

    [ObservableProperty]
    private string text = string.Empty;

    [ObservableProperty]
    private int number;

    [ObservableProperty]
    private Color color = Colors.Red;

    [ObservableProperty]
    private double thickness = 3;

    [ObservableProperty]
    private double opacity = 0.9;

    public SolidColorBrush StrokeBrush => new(color) { Opacity = opacity };
    public SolidColorBrush FillBrush => new(color) { Opacity = opacity * 0.8 };
}
