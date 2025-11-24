using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenshotProMax.Models;
using ScreenshotProMax.Services;
using ScreenshotProMax.ViewModels;
using MahApps.Metro.Controls;

namespace ScreenshotProMax.Views;

public partial class MainWindow : MetroWindow
{
    private AnnotationModel? _activeAnnotation;
    private bool _isDrawing;
    private bool _isDragging;
    private bool _isResizing;
    private bool _isErasing;
    private Point _lastMousePosition;
    private HotkeyService? _hotkeyService;
    private Cursor? _eraserCursor;

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded; 
        Closed += MainWindow_Closed;
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        CreateEraserCursor();

        // Find the radio button by name to avoid referencing generated field directly
        var arrow = FindName("ArrowRadio") as RadioButton;
        if (arrow != null)
        {
            arrow.IsChecked = true; // set after initialization so OverlayCanvas exists
        }
    }

    private void CreateEraserCursor()
    {
        // Erstelle einen benutzerdefinierten Radiergummi-Cursor
        try
        {
            // Erstelle einen visuellen Radiergummi
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // Zeichne einen Kreis mit X als Radiergummi-Symbol
                var center = new Point(16, 16);
                var radius = 12.0;
                
                // Äußerer Kreis
                drawingContext.DrawEllipse(
                    Brushes.Transparent, 
                    new Pen(Brushes.Red, 2), 
                    center, 
                    radius, 
                    radius
                );
                
                // Inneres X
                var offset = radius * 0.5;
                drawingContext.DrawLine(
                    new Pen(Brushes.Red, 2), 
                    new Point(center.X - offset, center.Y - offset), 
                    new Point(center.X + offset, center.Y + offset)
                );
                drawingContext.DrawLine(
                    new Pen(Brushes.Red, 2), 
                    new Point(center.X + offset, center.Y - offset), 
                    new Point(center.X - offset, center.Y + offset)
                );
            }

            var renderTargetBitmap = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);
            
            // Konvertiere zu Cursor (verwende das Zentrum als Hotspot)
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
            using (var stream = new System.IO.MemoryStream())
            {
                encoder.Save(stream);
                stream.Position = 0;
                
                // Cursor aus Stream erstellen - Fallback zu Cross wenn es nicht funktioniert
                try
                {
                    var iconHandle = System.Runtime.InteropServices.Marshal.GetHINSTANCE(typeof(MainWindow).Module);
                    _eraserCursor = Cursors.Cross; // Fallback
                }
                catch
                {
                    _eraserCursor = Cursors.Cross;
                }
            }
        }
        catch
        {
            // Fallback auf Cross-Cursor
            _eraserCursor = Cursors.Cross;
        }
    }

    private System.IO.MemoryStream BitmapToCursor(RenderTargetBitmap bitmap, int hotX, int hotY)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var stream = new System.IO.MemoryStream();
        encoder.Save(stream);
        stream.Position = 0;
        return stream;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Register global hotkey Ctrl+D
        _hotkeyService = new HotkeyService();
        var handle = new WindowInteropHelper(this).Handle;
        
        if (_hotkeyService.RegisterHotkey(handle, ModifierKeys.Control, Key.D))
        {
            _hotkeyService.HotkeyPressed += HotkeyService_HotkeyPressed;
        }
        else
        {
            MessageBox.Show("Hotkey Strg+D konnte nicht registriert werden.", "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _hotkeyService?.Dispose();
    }

    private async void HotkeyService_HotkeyPressed(object? sender, EventArgs e)
    {
        // Minimize window before capture
        WindowState = WindowState.Minimized;
        await System.Threading.Tasks.Task.Delay(200);

        // Show region selector
        await ViewModel.CaptureRegionCommand.ExecuteAsync(null);

        // Restore window if capture was successful
        if (ViewModel.HasImage)
        {
            WindowState = WindowState.Normal;
            Activate();
        }
    }

    private async void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Strg+C für Copy to Clipboard
        if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control && ViewModel.HasImage)
        {
            await ViewModel.CopyToClipboardCommand.ExecuteAsync(null);
            e.Handled = true;
        }
        // Strg+Z für Undo
        else if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control && ViewModel.CanUndo)
        {
            ViewModel.UndoCommand.Execute(null);
            e.Handled = true;
        }
        // Strg+Y für Redo
        else if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control && ViewModel.CanRedo)
        {
            ViewModel.RedoCommand.Execute(null);
            e.Handled = true;
        }
        // Delete-Taste zum Löschen der ausgewählten Annotation
        else if (e.Key == Key.Delete && ViewModel.SelectedAnnotation != null)
        {
            ViewModel.Annotations.Remove(ViewModel.SelectedAnnotation);
            ViewModel.SelectedAnnotation = null;
            e.Handled = true;
        }
        // Escape zum Abbrechen der Auswahl
        else if (e.Key == Key.Escape)
        {
            ViewModel.DeselectAll();
            e.Handled = true;
        }
    }

    private void OverlayCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!ViewModel.HasImage)
        {
            return;
        }

        var position = e.GetPosition(OverlayCanvas);

        // Radierer-Werkzeug
        if (ViewModel.CurrentTool == AnnotationType.Eraser)
        {
            _isErasing = true;
            var annotation = ViewModel.FindAnnotationAt(position);
            if (annotation != null)
            {
                ViewModel.EraseAnnotation(annotation);
            }
            return;
        }

        // Auswahl-Werkzeug
        if (ViewModel.CurrentTool == AnnotationType.Selection)
        {
            var selected = ViewModel.SelectAnnotationAt(position);
            if (selected != null)
            {
                _isDragging = true;
                _lastMousePosition = position;
                
                // Prüfe ob auf Resize-Handle geklickt wurde
                var bounds = selected.GetBounds();
                var handleRect = new Rect(
                    bounds.Right - 5, 
                    bounds.Bottom - 5, 
                    10, 
                    10
                );
                
                if (handleRect.Contains(position))
                {
                    _isResizing = true;
                    _isDragging = false;
                    Mouse.Capture(OverlayCanvas);
                }
                else
                {
                    Mouse.Capture(OverlayCanvas);
                }
            }
            else
            {
                ViewModel.DeselectAll();
            }
            return;
        }

        // Normale Zeichenwerkzeuge
        _activeAnnotation = ViewModel.BeginAnnotation();
        _activeAnnotation.Points.Add(position);

        switch (ViewModel.CurrentTool)
        {
            case AnnotationType.Line:
            case AnnotationType.Arrow:
                _activeAnnotation.Points.Add(position);
                _isDrawing = true;
                break;
            case AnnotationType.Text:
                // Text is placed at clicked position, editable via TextBox
                _isDrawing = false;
                break;
            case AnnotationType.Number:
                // Number is placed at clicked position
                _isDrawing = false;
                break;
        }
    }

    private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        var position = e.GetPosition(OverlayCanvas);

        // Radierer-Werkzeug - Löschen beim Überfahren
        if (_isErasing && e.LeftButton == MouseButtonState.Pressed)
        {
            var annotation = ViewModel.FindAnnotationAt(position);
            if (annotation != null)
            {
                ViewModel.EraseAnnotation(annotation);
            }
            return;
        }

        // Verschieben einer ausgewählten Annotation
        if (_isDragging && ViewModel.SelectedAnnotation != null)
        {
            var offset = position - _lastMousePosition;
            ViewModel.MoveSelectedAnnotation(offset);
            _lastMousePosition = position;
            return;
        }

        // Skalieren einer ausgewählten Annotation
        if (_isResizing && ViewModel.SelectedAnnotation != null)
        {
            var bounds = ViewModel.SelectedAnnotation.GetBounds();
            var center = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
            
            var oldDistance = (_lastMousePosition - center).Length;
            var newDistance = (position - center).Length;
            
            if (oldDistance > 0)
            {
                var scaleFactor = newDistance / oldDistance;
                ViewModel.ScaleSelectedAnnotation(scaleFactor, center);
            }
            
            _lastMousePosition = position;
            return;
        }

        // Normales Zeichnen
        if (!_isDrawing || _activeAnnotation == null)
        {
            return;
        }

        switch (ViewModel.CurrentTool)
        {
            case AnnotationType.Line:
            case AnnotationType.Arrow:
                if (_activeAnnotation.Points.Count >= 2)
                {
                    _activeAnnotation.Points[1] = position;
                }
                break;
        }
    }

    private void OverlayCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDrawing = false;
        _isDragging = false;
        _isResizing = false;
        _isErasing = false;
        _activeAnnotation = null;
        Mouse.Capture(null);
    }

    private void OverlayCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Nur zoomen wenn Strg gedrückt ist
        if (Keyboard.Modifiers == ModifierKeys.Control && ViewModel.HasImage)
        {
            if (e.Delta > 0)
            {
                ViewModel.IncreaseZoom();
            }
            else
            {
                ViewModel.DecreaseZoom();
            }
            e.Handled = true;
        }
    }

    private void ToolRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radio && radio.Tag is string tag && Enum.TryParse<AnnotationType>(tag, out var tool))
        {
            ViewModel.CurrentTool = tool;
            
            // Cursor ändern basierend auf dem Werkzeug
            if (tool == AnnotationType.Eraser)
            {
                OverlayCanvas.Cursor = _eraserCursor ?? Cursors.Cross;
            }
            else
            {
                OverlayCanvas.Cursor = Cursors.Arrow;
            }
            
            // Deselektiere beim Wechsel zu einem Zeichenwerkzeug
            if (tool != AnnotationType.Selection)
            {
                ViewModel.DeselectAll();
            }
        }
    }
}
