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
        CapturedImage = await _screenshotService.CapturePrimaryScreenAsync();
        ResetAnnotations();
    }

    [RelayCommand]
    private async Task CaptureAllScreensAsync()
    {
        CapturedImage = await _screenshotService.CaptureAllScreensAsync();
        ResetAnnotations();
    }

    [RelayCommand]
    private async Task CaptureActiveWindowAsync()
    {
        // Small delay to allow the window to become active
        await Task.Delay(100);
        CapturedImage = await _screenshotService.CaptureActiveWindowAsync();
        ResetAnnotations();
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
        var annotation = new AnnotationModel
        {
            Type = CurrentTool,
            Color = CurrentColor,
            Thickness = CurrentThickness,
            Opacity = CurrentOpacity
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
}
