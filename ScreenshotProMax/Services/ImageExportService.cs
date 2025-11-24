using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenshotProMax.Models;

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
                DrawLine(context, annotation, arrow: false);
                break;
            case AnnotationType.Arrow:
                DrawLine(context, annotation, arrow: true);
                break;
            case AnnotationType.Text:
                DrawText(context, annotation.Text, annotation);
                break;
            case AnnotationType.Number:
                DrawNumber(context, annotation);
                break;
        }
    }

    private static void DrawLine(DrawingContext context, AnnotationModel annotation, bool arrow)
    {
        if (annotation.Points.Count < 2)
        {
            return;
        }

        var p1 = annotation.Points[0];
        var p2 = annotation.Points[^1];
        var pen = new Pen(annotation.StrokeBrush, annotation.Thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = arrow ? PenLineCap.Triangle : PenLineCap.Round
        };
        context.DrawLine(pen, p1, p2);
    }

    private static void DrawText(DrawingContext context, string text, AnnotationModel annotation)
    {
        var formatted = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            18,
            annotation.StrokeBrush,
            1.25);

        var origin = annotation.Points[0];
        context.DrawText(formatted, origin);
    }

    private static void DrawNumber(DrawingContext context, AnnotationModel annotation)
    {
        var origin = annotation.Points[0];
        var size = 28 + annotation.Text.Length * 2;
        var background = new SolidColorBrush(annotation.Color)
        {
            Opacity = annotation.Opacity * 0.9
        };
        var border = new Pen(Brushes.White, 1.5);
        context.DrawEllipse(background, border, origin, size / 2, size / 2);
        DrawText(context, annotation.Text, annotation);
    }
}
