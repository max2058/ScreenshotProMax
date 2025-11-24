using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ScreenshotProMax.Models;
using ScreenshotProMax.ViewModels;

namespace ScreenshotProMax.Views;

public partial class MainWindow : Window
{
    private AnnotationModel? _activeAnnotation;
    private bool _isDrawing;

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
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
                if (string.IsNullOrWhiteSpace(_activeAnnotation.Text))
                {
                    _activeAnnotation.Text = "Text";
                }
                _activeAnnotation.Points.Add(position);
                _isDrawing = false;
                break;
            case AnnotationType.Number:
                _activeAnnotation.Points.Add(position);
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
