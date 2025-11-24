using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using ScreenshotProMax.Models;
using ScreenshotProMax.Services;
using ScreenshotProMax.ViewModels;
using MahApps.Metro.Controls;

namespace ScreenshotProMax.Views;

public partial class MainWindow : MetroWindow
{
    private AnnotationModel? _activeAnnotation;
    private bool _isDrawing;
    private HotkeyService? _hotkeyService;

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded; 
        Closed += MainWindow_Closed;
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

    private void OverlayCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!ViewModel.HasImage)
        {
            return;
        }

        _activeAnnotation = ViewModel.BeginAnnotation();
        var position = e.GetPosition(OverlayCanvas);
        _activeAnnotation.Points.Add(position);

        switch (ViewModel.CurrentTool)
        {
            case AnnotationType.Line:
            case AnnotationType.Arrow:
                _activeAnnotation.Points.Add(position);
                _isDrawing = true;
                break;
            case AnnotationType.Pen:
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
        if (!_isDrawing || _activeAnnotation == null)
        {
            return;
        }

        var position = e.GetPosition(OverlayCanvas);
        switch (ViewModel.CurrentTool)
        {
            case AnnotationType.Line:
            case AnnotationType.Arrow:
                if (_activeAnnotation.Points.Count >= 2)
                {
                    _activeAnnotation.Points[1] = position;
                }
                break;
            case AnnotationType.Pen:
                _activeAnnotation.Points.Add(position);
                break;
        }
    }

    private void OverlayCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDrawing = false;
        _activeAnnotation = null;
    }

    private void ToolRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radio && radio.Tag is string tag && Enum.TryParse<AnnotationType>(tag, out var tool))
        {
            ViewModel.CurrentTool = tool;
        }
    }

    private void ColorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item && item.Tag is string hex)
        {
            if (ColorConverter.ConvertFromString(hex) is Color color)
            {
                ViewModel.CurrentColor = color;
            }
        }
    }
}
