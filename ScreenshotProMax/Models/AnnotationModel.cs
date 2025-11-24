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

    public AnnotationModel()
    {
        // Reagiere auf Änderungen der Points-Collection
        Points.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(ArrowHeadPoints));
            OnPropertyChanged(nameof(ArrowHeadTip));
            OnPropertyChanged(nameof(ArrowHeadLeft));
            OnPropertyChanged(nameof(ArrowHeadRight));
        };
    }

    partial void OnThicknessChanged(double value)
    {
        // Aktualisiere Pfeilspitzen-Punkte wenn Thickness sich ändert
        OnPropertyChanged(nameof(ArrowHeadPoints));
        OnPropertyChanged(nameof(ArrowHeadTip));
        OnPropertyChanged(nameof(ArrowHeadLeft));
        OnPropertyChanged(nameof(ArrowHeadRight));
    }

    public SolidColorBrush StrokeBrush => new(Color) { Opacity = Opacity };
    public SolidColorBrush FillBrush => new(Color) { Opacity = Opacity * 0.8 };

    // PointCollection für die Pfeilspitze (für XAML Binding)
    public PointCollection ArrowHeadPoints
    {
        get
        {
            var points = new PointCollection();
            
            if (Points.Count < 2 || Type != AnnotationType.Arrow)
            {
                return points;
            }

            Point start = Points[0];
            Point end = Points[1];

            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double length = System.Math.Sqrt(dx * dx + dy * dy);

            if (length == 0)
            {
                points.Add(end);
                points.Add(end);
                points.Add(end);
                return points;
            }

            dx /= length;
            dy /= length;

            double arrowLength = Thickness * 4;
            double arrowWidth = Thickness * 2;

            double baseX = end.X - dx * arrowLength;
            double baseY = end.Y - dy * arrowLength;

            // Spitze
            points.Add(end);
            
            // Links
            double leftX = baseX + dy * arrowWidth;
            double leftY = baseY - dx * arrowWidth;
            points.Add(new Point(leftX, leftY));
            
            // Rechts
            double rightX = baseX - dy * arrowWidth;
            double rightY = baseY + dx * arrowWidth;
            points.Add(new Point(rightX, rightY));

            return points;
        }
    }

    // Berechnet die Punkte für die Pfeilspitze
    public Point ArrowHeadTip
    {
        get
        {
            if (Points.Count < 2 || Type != AnnotationType.Arrow)
                return new Point(0, 0);
            return Points[1];
        }
    }

    public Point ArrowHeadLeft
    {
        get
        {
            if (Points.Count < 2 || Type != AnnotationType.Arrow)
                return new Point(0, 0);

            Point start = Points[0];
            Point end = Points[1];

            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double length = System.Math.Sqrt(dx * dx + dy * dy);

            if (length == 0)
                return end;

            dx /= length;
            dy /= length;

            double arrowLength = Thickness * 4;
            double arrowWidth = Thickness * 2;

            double baseX = end.X - dx * arrowLength;
            double baseY = end.Y - dy * arrowLength;

            double leftX = baseX + dy * arrowWidth;
            double leftY = baseY - dx * arrowWidth;

            return new Point(leftX, leftY);
        }
    }

    public Point ArrowHeadRight
    {
        get
        {
            if (Points.Count < 2 || Type != AnnotationType.Arrow)
                return new Point(0, 0);

            Point start = Points[0];
            Point end = Points[1];

            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double length = System.Math.Sqrt(dx * dx + dy * dy);

            if (length == 0)
                return end;

            dx /= length;
            dy /= length;

            double arrowLength = Thickness * 4;
            double arrowWidth = Thickness * 2;

            double baseX = end.X - dx * arrowLength;
            double baseY = end.Y - dy * arrowLength;

            double rightX = baseX - dy * arrowWidth;
            double rightY = baseY + dx * arrowWidth;

            return new Point(rightX, rightY);
        }
    }

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

        // Unterschiedliche Bounding Boxes für verschiedene Annotation-Typen
        double padding;
        double width;
        double height;

        switch (Type)
        {
            case AnnotationType.Text:
                // Für Text: Schätze die Größe basierend auf Textlänge
                // Mindestbreite 80px (wie im XAML MinWidth), aber skalierbar
                var estimatedTextWidth = System.Math.Max(80, Text.Length * 10);
                var estimatedTextHeight = 30; // Geschätzte Höhe für FontSize 16
                
                width = estimatedTextWidth * Scale;
                height = estimatedTextHeight * Scale;
                padding = 8; // Border Padding aus XAML
                
                return new Rect(
                    minX - padding,
                    minY - padding,
                    width + 2 * padding,
                    height + 2 * padding
                );

            case AnnotationType.Number:
                // Für Nummern: Feste Größe basierend auf Badge-Dimensionen
                var estimatedNumberWidth = 32; // Geschätzte Breite für Nummer
                var estimatedNumberHeight = 32; // Runder Badge
                
                width = estimatedNumberWidth * Scale;
                height = estimatedNumberHeight * Scale;
                padding = 8; // Border Padding aus XAML
                
                return new Rect(
                    minX - padding,
                    minY - padding,
                    width + 2 * padding,
                    height + 2 * padding
                );

            default:
                // Für Linien und Pfeile: Nutze die Thickness als Padding
                padding = Thickness * 2;
                
                return new Rect(
                    minX - padding,
                    minY - padding,
                    maxX - minX + 2 * padding,
                    maxY - minY + 2 * padding
                );
        }
    }

    // Prüft ob ein Punkt innerhalb der Annotation liegt
    public bool HitTest(Point point)
    {
        var bounds = GetBounds();
        return bounds.Contains(point);
    }
}
