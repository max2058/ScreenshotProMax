using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ScreenshotProMax.Models;
using ScreenshotProMax.Services;
using System.Windows;
using ScreenshotProMax.Views;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ScreenshotProMax.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ScreenshotService _screenshotService;
    private readonly ImageExportService _exportService;
    private readonly Stack<AnnotationModel> _undoStack = new();
    private readonly Stack<AnnotationModel> _redoStack = new();
    private const int MaxUndoSteps = 10;

    public MainViewModel()
    {
        _screenshotService = new ScreenshotService();
        _exportService = new ImageExportService();
        CurrentTool = AnnotationType.Arrow;
        CurrentColor = Colors.Red;
        CurrentThickness = 3;
        CurrentOpacity = 0.9;
        ZoomLevel = 1.0;
    }

    [ObservableProperty]
    private BitmapSource? capturedImage;

    [ObservableProperty]
    private ObservableCollection<AnnotationModel> annotations = new();

    [ObservableProperty]
    private AnnotationType currentTool;

    // Explicit Color property to avoid System.Drawing/System.Windows ambiguity in generated code
    private Color _currentColor = Colors.Red;
    public Color CurrentColor
    {
        get => _currentColor;
        set => SetProperty(ref _currentColor, value);
    }

    [ObservableProperty]
    private double currentThickness;

    [ObservableProperty]
    private double currentOpacity;

    [ObservableProperty]
    private int nextNumber = 1;

    [ObservableProperty]
    private double zoomLevel;

    [ObservableProperty]
    private AnnotationModel? selectedAnnotation;

    [ObservableProperty]
    private ShapeStyle currentShapeStyle = ShapeStyle.StrokeOnly;

    public bool HasImage => CapturedImage != null;
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    partial void OnCapturedImageChanged(BitmapSource? oldValue, BitmapSource? newValue)
    {
        OnPropertyChanged(nameof(HasImage));
        SaveCommand.NotifyCanExecuteChanged();
        ClearAnnotationsCommand.NotifyCanExecuteChanged();
        CopyToClipboardCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task CaptureAsync()
    {
        // Show screen selector overlay
        var screenSelector = new ScreenSelectorWindow();
        if (screenSelector.ShowDialog() == true && screenSelector.SelectedScreen != null)
        {
            CapturedImage = await _screenshotService.CaptureScreenAsync(screenSelector.SelectedScreen);
            ResetAnnotations();
        }
    }

    [RelayCommand]
    private async Task CaptureActiveWindowAsync()
    {
        System.Diagnostics.Debug.WriteLine("=== CaptureActiveWindowAsync started ===");
        
        // Minimize main window if it's visible
        if (Application.Current.MainWindow?.WindowState == WindowState.Normal)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
            System.Diagnostics.Debug.WriteLine("Main window minimized");
        }

        // Small delay to allow the main window to minimize
        await Task.Delay(150);
        
        // Show window selector overlay
        System.Diagnostics.Debug.WriteLine("Showing WindowSelectorWindow");
        var windowSelector = new WindowSelectorWindow();
        var result = windowSelector.ShowDialog();
        
        System.Diagnostics.Debug.WriteLine($"WindowSelectorWindow closed with result: {result}");
        System.Diagnostics.Debug.WriteLine($"SelectedWindowRect: {windowSelector.SelectedWindowRect}");
        
        if (result == true && windowSelector.SelectedWindowRect.HasValue)
        {
            var rect = windowSelector.SelectedWindowRect.Value;
            System.Diagnostics.Debug.WriteLine($"Capturing region: X={rect.X}, Y={rect.Y}, W={rect.Width}, H={rect.Height}");
            
            CapturedImage = await _screenshotService.CaptureRegionAsync(rect);
            System.Diagnostics.Debug.WriteLine($"Screenshot captured: {CapturedImage != null}");
            
            ResetAnnotations();

            // Restore main window
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                Application.Current.MainWindow.Activate();
                System.Diagnostics.Debug.WriteLine("Main window restored");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Window selection cancelled or no window selected");
            
            // Restore main window even if cancelled
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                Application.Current.MainWindow.Activate();
            }
        }
        
        System.Diagnostics.Debug.WriteLine("=== CaptureActiveWindowAsync finished ===");
    }

    [RelayCommand]
    private async Task CaptureRegionAsync()
    {
        var regionSelector = new RegionSelectorWindow();
        if (regionSelector.ShowDialog() == true && regionSelector.SelectedRegion.HasValue)
        {
            var region = regionSelector.SelectedRegion.Value;
            CapturedImage = await _screenshotService.CaptureRegionAsync(region);
            ResetAnnotations();
        }
    }

    [RelayCommand(CanExecute = nameof(HasImage))]
    private void ClearAnnotations()
    {
        ResetAnnotations();
    }

    [RelayCommand(CanExecute = nameof(HasImage))]
    private async Task SaveAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg",
            FileName = $"Screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png"
        };

        if (dialog.ShowDialog() == true && CapturedImage != null)
        {
            await _exportService.SaveAsync(dialog.FileName, CapturedImage, Annotations);
        }
    }

    [RelayCommand(CanExecute = nameof(HasImage))]
    private async Task CopyToClipboardAsync()
    {
        if (CapturedImage == null) return;

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var bitmap = _exportService.CreateAnnotatedBitmap(CapturedImage, Annotations);
            Clipboard.SetImage(bitmap);
        });
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (_undoStack.Count == 0) return;

        var annotation = _undoStack.Pop();
        _redoStack.Push(annotation);
        Annotations.Remove(annotation);

        // Nummerierung anpassen
        if (annotation.Type == AnnotationType.Number)
        {
            NextNumber = Annotations.Where(a => a.Type == AnnotationType.Number)
                                   .Select(a => a.Number)
                                   .DefaultIfEmpty(0)
                                   .Max() + 1;
        }

        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (_redoStack.Count == 0) return;

        var annotation = _redoStack.Pop();
        _undoStack.Push(annotation);
        Annotations.Add(annotation);

        // Nummerierung anpassen
        if (annotation.Type == AnnotationType.Number)
        {
            NextNumber = Annotations.Where(a => a.Type == AnnotationType.Number)
                                   .Select(a => a.Number)
                                   .DefaultIfEmpty(0)
                                   .Max() + 1;
        }

        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    public AnnotationModel BeginAnnotation()
    {
        // Deselektiere alle vorherigen Anmerkungen
        DeselectAll();

        var annotation = new AnnotationModel
        {
            Type = CurrentTool,
            Color = CurrentColor,
            Thickness = CurrentThickness,
            Opacity = CurrentOpacity,
            ShapeStyle = CurrentShapeStyle
        };

        if (CurrentTool == AnnotationType.Number)
        {
            annotation.Number = NextNumber++;
            annotation.Text = annotation.Number.ToString();
        }
        else if (CurrentTool == AnnotationType.Text)
        {
            annotation.Text = "Text eingeben...";
        }

        Annotations.Add(annotation);
        
        // Add to undo stack
        _undoStack.Push(annotation);
        if (_undoStack.Count > MaxUndoSteps)
        {
            var items = _undoStack.ToList();
            _undoStack.Clear();
            for (int i = items.Count - MaxUndoSteps; i < items.Count; i++)
            {
                _undoStack.Push(items[i]);
            }
        }
        
        // Clear redo stack when new annotation is added
        _redoStack.Clear();
        
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        
        return annotation;
    }

    public void DeselectAll()
    {
        foreach (var annotation in Annotations)
        {
            annotation.IsSelected = false;
        }
        SelectedAnnotation = null;
    }

    public AnnotationModel? SelectAnnotationAt(Point point)
    {
        // Deselektiere vorherige Auswahl
        DeselectAll();

        // Suche von hinten nach vorne (neueste Anmerkungen zuerst)
        for (int i = Annotations.Count - 1; i >= 0; i--)
        {
            var annotation = Annotations[i];
            if (annotation.HitTest(point))
            {
                annotation.IsSelected = true;
                SelectedAnnotation = annotation;
                return annotation;
            }
        }

        return null;
    }

    public AnnotationModel? FindAnnotationAt(Point point)
    {
        // Suche von hinten nach vorne (neueste Anmerkungen zuerst)
        for (int i = Annotations.Count - 1; i >= 0; i--)
        {
            var annotation = Annotations[i];
            if (annotation.HitTest(point))
            {
                return annotation;
            }
        }

        return null;
    }

    public void MoveSelectedAnnotation(Vector offset)
    {
        if (SelectedAnnotation == null) return;

        for (int i = 0; i < SelectedAnnotation.Points.Count; i++)
        {
            SelectedAnnotation.Points[i] = new Point(
                SelectedAnnotation.Points[i].X + offset.X,
                SelectedAnnotation.Points[i].Y + offset.Y
            );
        }
    }

    public void ScaleSelectedAnnotation(double scaleFactor, Point center)
    {
        if (SelectedAnnotation == null) return;

        // Aktualisiere Scale-Eigenschaft
        SelectedAnnotation.Scale *= scaleFactor;

        // Skaliere Punkte relativ zum Zentrum
        for (int i = 0; i < SelectedAnnotation.Points.Count; i++)
        {
            var point = SelectedAnnotation.Points[i];
            var dx = point.X - center.X;
            var dy = point.Y - center.Y;
            
            SelectedAnnotation.Points[i] = new Point(
                center.X + dx * scaleFactor,
                center.Y + dy * scaleFactor
            );
        }

        // Skaliere auch die Thickness
        SelectedAnnotation.Thickness *= scaleFactor;
    }

    public void ResetAnnotations()
    {
        Annotations.Clear();
        _undoStack.Clear();
        _redoStack.Clear();
        NextNumber = 1;
        
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    public void IncreaseZoom()
    {
        if (ZoomLevel < 5.0) // Max 500%
        {
            ZoomLevel = Math.Min(5.0, ZoomLevel + 0.1);
        }
    }

    public void DecreaseZoom()
    {
        if (ZoomLevel > 0.1) // Min 10%
        {
            ZoomLevel = Math.Max(0.1, ZoomLevel - 0.1);
        }
    }

    public void ResetZoom()
    {
        ZoomLevel = 1.0;
    }

    public void EraseAnnotation(AnnotationModel annotation)
    {
        if (annotation == null) return;

        Annotations.Remove(annotation);
        
        // Nummerierung anpassen wenn eine Nummer gelöscht wurde
        if (annotation.Type == AnnotationType.Number)
        {
            NextNumber = Annotations.Where(a => a.Type == AnnotationType.Number)
                                   .Select(a => a.Number)
                                   .DefaultIfEmpty(0)
                                   .Max() + 1;
        }

        // Füge zur Undo-Stack hinzu für mögliches Rückgängigmachen
        _undoStack.Push(annotation);
        if (_undoStack.Count > MaxUndoSteps)
        {
            var items = _undoStack.ToList();
            _undoStack.Clear();
            for (int i = items.Count - MaxUndoSteps; i < items.Count; i++)
            {
                _undoStack.Push(items[i]);
            }
        }

        OnPropertyChanged(nameof(CanUndo));
        UndoCommand.NotifyCanExecuteChanged();
    }
}
