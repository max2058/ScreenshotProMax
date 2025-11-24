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

namespace ScreenshotProMax.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ScreenshotService _screenshotService;
    private readonly ImageExportService _exportService;

    public MainViewModel()
    {
        _screenshotService = new ScreenshotService();
        _exportService = new ImageExportService();
        CurrentTool = AnnotationType.Arrow;
        CurrentColor = Colors.Red;
        CurrentThickness = 3;
        CurrentOpacity = 0.9;
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

    public bool HasImage => CapturedImage != null;

    partial void OnCapturedImageChanged(BitmapSource? oldValue, BitmapSource? newValue)
    {
        OnPropertyChanged(nameof(HasImage));
        SaveCommand.NotifyCanExecuteChanged();
        ClearAnnotationsCommand.NotifyCanExecuteChanged();
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
        return annotation;
    }

    public void ResetAnnotations()
    {
        Annotations.Clear();
        NextNumber = 1;
    }
}
