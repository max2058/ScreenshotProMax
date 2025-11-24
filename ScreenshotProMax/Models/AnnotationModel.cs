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
    Selection,
    Eraser
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

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private double scale = 1.0;

    public SolidColorBrush StrokeBrush => new(Color) { Opacity = Opacity };
    public SolidColorBrush FillBrush => new(Color) { Opacity = Opacity * 0.8 };

    // Bounding Box für Hit-Testing und Auswahl
    public Rect GetBounds()
    {
        if (Points.Count == 0)
            return Rect.Empty;

        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (var point in Points)
        {
            minX = System.Math.Min(minX, point.X);
            minY = System.Math.Min(minY, point.Y);
            maxX = System.Math.Max(maxX, point.X);
            maxY = System.Math.Max(maxY, point.Y);
        }

        // Padding für bessere Auswahl bei kleinen Objekten
        double padding = Type == AnnotationType.Number || Type == AnnotationType.Text ? 20 : Thickness * 2;
        
        return new Rect(
            minX - padding,
            minY - padding,
            maxX - minX + 2 * padding,
            maxY - minY + 2 * padding
        );
    }

    // Prüft ob ein Punkt innerhalb der Annotation liegt
    public bool HitTest(Point point)
    {
        var bounds = GetBounds();
        return bounds.Contains(point);
    }
}
