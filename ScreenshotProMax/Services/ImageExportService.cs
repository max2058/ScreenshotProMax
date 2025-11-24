using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenshotProMax.Models;
using System; // für Math

namespace ScreenshotProMax.Services;

public class ImageExportService
{
    public Task SaveAsync(string filePath, BitmapSource background, ObservableCollection<AnnotationModel> annotations)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var bitmap = CreateAnnotatedBitmap(background, annotations);

            BitmapEncoder encoder = Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => new JpegBitmapEncoder(),
                _ => new PngBitmapEncoder()
            };

            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(filePath);
            encoder.Save(stream);
        }).Task;
    }

    public BitmapSource CreateAnnotatedBitmap(BitmapSource background, ObservableCollection<AnnotationModel> annotations)
    {
        var drawingVisual = new DrawingVisual();
        using (var context = drawingVisual.RenderOpen())
        {
            var rect = new Rect(0, 0, background.PixelWidth, background.PixelHeight);
            context.DrawImage(background, rect);

            foreach (var annotation in annotations)
            {
                DrawAnnotation(context, annotation);
            }
        }

        var rtb = new RenderTargetBitmap(background.PixelWidth, background.PixelHeight, background.DpiX, background.DpiY, PixelFormats.Pbgra32);
        rtb.Render(drawingVisual);
        return rtb;
    }

    private static void DrawAnnotation(DrawingContext context, AnnotationModel annotation)
    {
        if (annotation.Points.Count == 0)
        {
            return;
        }

        switch (annotation.Type)
        {
            case AnnotationType.Line:
                DrawLineOrArrow(context, annotation, arrow: false);
                break;
            case AnnotationType.Arrow:
                DrawLineOrArrow(context, annotation, arrow: true);
                break;
            case AnnotationType.Text:
                DrawText(context, annotation);
                break;
            case AnnotationType.Number:
                DrawNumber(context, annotation);
                break;
        }
    }

    private static void DrawLineOrArrow(DrawingContext context, AnnotationModel annotation, bool arrow)
    {
        if (annotation.Points.Count < 2)
        {
            return;
        }

        var p1 = annotation.Points[0];
        var p2 = annotation.Points[^1];
        double thickness = annotation.Thickness * annotation.Scale; // berücksichtige Skalierung
        var pen = new Pen(annotation.StrokeBrush, thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round // eigene Pfeilspitze zeichnen
        };
        context.DrawLine(pen, p1, p2);

        if (arrow)
        {
            // Pfeilspitze wie im UI (Thickness-basierte Berechnung, + Scale)
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < double.Epsilon) return;
            dx /= length; dy /= length;

            double arrowLength = thickness * 4; // konsistent mit UI Logic (Thickness * 4)
            double arrowWidth = thickness * 2;  // Thickness * 2
            double baseX = p2.X - dx * arrowLength;
            double baseY = p2.Y - dy * arrowLength;

            // Basis-Punkte seitlich
            var left = new Point(baseX + dy * arrowWidth, baseY - dx * arrowWidth);
            var right = new Point(baseX - dy * arrowWidth, baseY + dx * arrowWidth);

            var geo = new StreamGeometry();
            using (var gctx = geo.Open())
            {
                gctx.BeginFigure(p2, isFilled: true, isClosed: true);
                gctx.LineTo(left, true, false);
                gctx.LineTo(right, true, false);
            }
            geo.Freeze();
            context.DrawGeometry(annotation.StrokeBrush, null, geo);
        }
    }

    private static void DrawText(DrawingContext context, AnnotationModel annotation)
    {
        // FontSize analog UI (16) skalieren
        double fontSize = 16 * annotation.Scale;
        var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        var formatted = new FormattedText(
            annotation.Text,
            System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.White,
            1.25)
        {
            TextAlignment = TextAlignment.Left
        };

        var origin = annotation.Points[0];
        double paddingX = 8 * annotation.Scale;
        double paddingY = 4 * annotation.Scale;
        double minWidth = 80 * annotation.Scale;
        double cornerRadius = 4 * annotation.Scale;

        double boxWidth = Math.Max(minWidth, formatted.Width + paddingX * 2);
        double boxHeight = formatted.Height + paddingY * 2;
        var rect = new Rect(origin.X, origin.Y, boxWidth, boxHeight);

        // Hintergrund & Rahmen (FillBrush & Weißer Rand 2px)
        var background = new SolidColorBrush(annotation.Color) { Opacity = annotation.Opacity * 0.8 };
        var borderPen = new Pen(Brushes.White, 2 * annotation.Scale);
        context.DrawRoundedRectangle(background, borderPen, rect, cornerRadius, cornerRadius);

        // Text zeichnen (oben links + Padding)
        var textPoint = new Point(origin.X + paddingX, origin.Y + paddingY - 1); // -1 leichter optischer Ausgleich
        context.DrawText(formatted, textPoint);
    }

    private static void DrawNumber(DrawingContext context, AnnotationModel annotation)
    {
        var topLeft = annotation.Points[0];

        double fontSize = 14 * annotation.Scale;
        var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        var formatted = new FormattedText(
            annotation.Text,
            System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.White,
            1.25);

        double paddingX = 8 * annotation.Scale;
        double paddingY = 4 * annotation.Scale;
        double cornerRadius = 16 * annotation.Scale; // entspricht UI Border CornerRadius

        double boxWidth = formatted.Width + paddingX * 2;
        double boxHeight = formatted.Height + paddingY * 2;
        var rect = new Rect(topLeft.X, topLeft.Y, boxWidth, boxHeight);

        var background = new SolidColorBrush(annotation.Color) { Opacity = annotation.Opacity * 0.9 };
        var borderPen = new Pen(Brushes.White, 2 * annotation.Scale);
        context.DrawRoundedRectangle(background, borderPen, rect, cornerRadius, cornerRadius);

        double textX = topLeft.X + (boxWidth - formatted.Width) / 2;
        double textY = topLeft.Y + (boxHeight - formatted.Height) / 2 - 1; // leichter optischer Ausgleich
        context.DrawText(formatted, new Point(textX, textY));
    }
}
