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
    Rectangle,
    Ellipse,
    Selection,
    Eraser
}

public enum ShapeStyle
{
    StrokeOnly,      // Nur Rahmen
    StrokeAndFill,   // Rahmen und Füllung
    FillOnly         // Nur Füllung
}

public enum ResizeHandleType
{
    TopLeft,
    TopRight, 
    BottomLeft,
    BottomRight,
    MiddleLeft,
    MiddleRight,
    MiddleTop,
    MiddleBottom,
    StartPoint,    // Für Linien/Pfeile - Startpunkt
    EndPoint       // Für Linien/Pfeile - Endpunkt
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

    [ObservableProperty]
    private ShapeStyle shapeStyle = ShapeStyle.StrokeOnly;

    [ObservableProperty]
    private double fontSize = 16.0;  // Für Text-Annotationen

    public AnnotationModel()
    {
        // Reagiere auf Änderungen der Points-Collection
        Points.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(ArrowHeadPoints));
            OnPropertyChanged(nameof(ArrowHeadTip));
            OnPropertyChanged(nameof(ArrowHeadLeft));
            OnPropertyChanged(nameof(ArrowHeadRight));
            OnPropertyChanged(nameof(ShapeBounds));
            OnPropertyChanged(nameof(ResizeHandles));
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

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(ResizeHandles));
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

    // Berechnet Resize-Handles basierend auf dem Typ der Annotation
    public ObservableCollection<ResizeHandleInfo> ResizeHandles
    {
        get
        {
            var handles = new ObservableCollection<ResizeHandleInfo>();

            if (!IsSelected || Points.Count == 0)
                return handles;

            switch (Type)
            {
                case AnnotationType.Line:
                case AnnotationType.Arrow:
                    // Start- und Endpunkt als Handles
                    if (Points.Count >= 2)
                    {
                        handles.Add(new ResizeHandleInfo { Position = Points[0], Type = ResizeHandleType.StartPoint });
                        handles.Add(new ResizeHandleInfo { Position = Points[1], Type = ResizeHandleType.EndPoint });
                    }
                    break;

                case AnnotationType.Rectangle:
                case AnnotationType.Ellipse:
                    // 8 Handles um das Shape herum
                    var bounds = ShapeBounds;
                    if (!bounds.IsEmpty)
                    {
                        handles.Add(new ResizeHandleInfo { Position = new Point(bounds.Left, bounds.Top), Type = ResizeHandleType.TopLeft });
                        handles.Add(new ResizeHandleInfo { Position = new Point(bounds.Right, bounds.Top), Type = ResizeHandleType.TopRight });
                        handles.Add(new ResizeHandleInfo { Position = new Point(bounds.Left, bounds.Bottom), Type = ResizeHandleType.BottomLeft });
                        handles.Add(new ResizeHandleInfo { Position = new Point(bounds.Right, bounds.Bottom), Type = ResizeHandleType.BottomRight });
                        handles.Add(new ResizeHandleInfo { Position = new Point(bounds.Left, bounds.Top + bounds.Height / 2), Type = ResizeHandleType.MiddleLeft });
                        handles.Add(new ResizeHandleInfo { Position = new Point(bounds.Right, bounds.Top + bounds.Height / 2), Type = ResizeHandleType.MiddleRight });
                        handles.Add(new ResizeHandleInfo { Position = new Point(bounds.Left + bounds.Width / 2, bounds.Top), Type = ResizeHandleType.MiddleTop });
                        handles.Add(new ResizeHandleInfo { Position = new Point(bounds.Left + bounds.Width / 2, bounds.Bottom), Type = ResizeHandleType.MiddleBottom });
                    }
                    break;

                case AnnotationType.Text:
                case AnnotationType.Number:
                    // 4 Eck-Handles für Text/Nummer
                    var textBounds = GetBounds();
                    if (!textBounds.IsEmpty)
                    {
                        handles.Add(new ResizeHandleInfo { Position = new Point(textBounds.Left, textBounds.Top), Type = ResizeHandleType.TopLeft });
                        handles.Add(new ResizeHandleInfo { Position = new Point(textBounds.Right, textBounds.Top), Type = ResizeHandleType.TopRight });
                        handles.Add(new ResizeHandleInfo { Position = new Point(textBounds.Left, textBounds.Bottom), Type = ResizeHandleType.BottomLeft });
                        handles.Add(new ResizeHandleInfo { Position = new Point(textBounds.Right, textBounds.Bottom), Type = ResizeHandleType.BottomRight });
                    }
                    break;
            }

            return handles;
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
                // Für Text: Schätze die Größe basierend auf Textlänge und Font-Size
                var estimatedTextWidth = System.Math.Max(80, Text.Length * FontSize * 0.6);
                var estimatedTextHeight = FontSize * 1.5;
                
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
                // Für Nummern: Feste Größe basierend auf Badge-Dimensionen und Scale
                var estimatedNumberWidth = 32 * Scale;
                var estimatedNumberHeight = 32 * Scale;
                
                width = estimatedNumberWidth;
                height = estimatedNumberHeight;
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

    // Prüft ob ein Punkt auf einem Resize-Handle liegt
    public ResizeHandleInfo? HitTestHandle(Point point)
    {
        const double handleSize = 8.0; // Größe der Handles
        
        foreach (var handle in ResizeHandles)
        {
            var handleBounds = new Rect(
                handle.Position.X - handleSize / 2,
                handle.Position.Y - handleSize / 2,
                handleSize,
                handleSize
            );

            if (handleBounds.Contains(point))
            {
                return handle;
            }
        }

        return null;
    }

    // Für Rechteck und Ellipse: Berechne Bounds aus zwei Punkten
    public Rect ShapeBounds
    {
        get
        {
            if (Points.Count < 2)
                return Rect.Empty;

            var p1 = Points[0];
            var p2 = Points[1];

            return new Rect(
                System.Math.Min(p1.X, p2.X),
                System.Math.Min(p1.Y, p2.Y),
                System.Math.Abs(p2.X - p1.X),
                System.Math.Abs(p2.Y - p1.Y)
            );
        }
    }

    // Hilfsmethode zum Anpassen von Punkten beim Resize
    public void ResizeWithHandle(ResizeHandleType handleType, Point newPosition)
    {
        switch (Type)
        {
            case AnnotationType.Line:
            case AnnotationType.Arrow:
                if (handleType == ResizeHandleType.StartPoint && Points.Count >= 1)
                {
                    Points[0] = newPosition;
                }
                else if (handleType == ResizeHandleType.EndPoint && Points.Count >= 2)
                {
                    Points[1] = newPosition;
                }
                break;

            case AnnotationType.Rectangle:
            case AnnotationType.Ellipse:
                ResizeShape(handleType, newPosition);
                break;

            case AnnotationType.Text:
            case AnnotationType.Number:
                ResizeText(handleType, newPosition);
                break;
        }
    }

    private void ResizeShape(ResizeHandleType handleType, Point newPosition)
    {
        if (Points.Count < 2) return;

        var bounds = ShapeBounds;
        var p1 = Points[0];
        var p2 = Points[1];

        switch (handleType)
        {
            case ResizeHandleType.TopLeft:
                Points[0] = new Point(newPosition.X, newPosition.Y);
                break;
            case ResizeHandleType.TopRight:
                Points[0] = new Point(bounds.Left, newPosition.Y);
                Points[1] = new Point(newPosition.X, bounds.Bottom);
                break;
            case ResizeHandleType.BottomLeft:
                Points[0] = new Point(newPosition.X, bounds.Top);
                Points[1] = new Point(bounds.Right, newPosition.Y);
                break;
            case ResizeHandleType.BottomRight:
                Points[1] = new Point(newPosition.X, newPosition.Y);
                break;
            case ResizeHandleType.MiddleLeft:
                Points[0] = new Point(newPosition.X, bounds.Top);
                break;
            case ResizeHandleType.MiddleRight:
                Points[1] = new Point(newPosition.X, bounds.Bottom);
                break;
            case ResizeHandleType.MiddleTop:
                Points[0] = new Point(bounds.Left, newPosition.Y);
                break;
            case ResizeHandleType.MiddleBottom:
                Points[1] = new Point(bounds.Right, newPosition.Y);
                break;
        }
    }

    private void ResizeText(ResizeHandleType handleType, Point newPosition)
    {
        if (Points.Count < 1) return;

        var currentBounds = GetBounds();
        var currentPos = Points[0];

        // Berechne neue Größe basierend auf Handle-Position
        double deltaX = 0;
        double deltaY = 0;

        switch (handleType)
        {
            case ResizeHandleType.TopLeft:
                deltaX = currentBounds.Right - newPosition.X;
                deltaY = currentBounds.Bottom - newPosition.Y;
                Points[0] = newPosition;
                break;
            case ResizeHandleType.TopRight:
                deltaX = newPosition.X - currentBounds.Left;
                deltaY = currentBounds.Bottom - newPosition.Y;
                Points[0] = new Point(currentBounds.Left, newPosition.Y);
                break;
            case ResizeHandleType.BottomLeft:
                deltaX = currentBounds.Right - newPosition.X;
                deltaY = newPosition.Y - currentBounds.Top;
                Points[0] = new Point(newPosition.X, currentBounds.Top);
                break;
            case ResizeHandleType.BottomRight:
                deltaX = newPosition.X - currentBounds.Left;
                deltaY = newPosition.Y - currentBounds.Top;
                break;
        }

        // Anpassen der Font-Size für Text basierend auf der Größenänderung
        if (Type == AnnotationType.Text)
        {
            var scaleFactor = System.Math.Max(deltaX / currentBounds.Width, deltaY / currentBounds.Height);
            if (scaleFactor > 0.1 && scaleFactor < 10) // Begrenze den Skalierungsfaktor
            {
                FontSize = System.Math.Max(8, System.Math.Min(72, FontSize * scaleFactor));
            }
        }
        else if (Type == AnnotationType.Number)
        {
            // Für Nummern: Anpassung der Scale-Eigenschaft
            var scaleFactor = System.Math.Max(deltaX / currentBounds.Width, deltaY / currentBounds.Height);
            if (scaleFactor > 0.1 && scaleFactor < 5)
            {
                Scale = System.Math.Max(0.5, System.Math.Min(3.0, Scale * scaleFactor));
            }
        }
    }
}

// Hilfsklasse für Resize-Handle-Informationen
public class ResizeHandleInfo
{
    public Point Position { get; set; }
    public ResizeHandleType Type { get; set; }
}
