using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Interop;
using DrawingRectangle = System.Drawing.Rectangle;
using ShapesRectangle = System.Windows.Shapes.Rectangle;

namespace ScreenshotProMax.Views
{
    public partial class RegionSelectorWindow : Window
    {
        private System.Windows.Point _startPoint;
        private ShapesRectangle? _selectionRectangle;
        private bool _isSelecting;
        private int _offsetX;
        private int _offsetY;

        public DrawingRectangle? SelectedRegion { get; private set; }

        public RegionSelectorWindow()
        {
            InitializeComponent();
            Loaded += RegionSelectorWindow_Loaded;
        }

        private void RegionSelectorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Calculate bounds across ALL screens including negative coordinates
            var screens = System.Windows.Forms.Screen.AllScreens;
            if (screens.Length == 0)
            {
                Close();
                return;
            }

            // Find absolute bounds of all screens
            int minX = screens.Min(s => s.Bounds.X);
            int minY = screens.Min(s => s.Bounds.Y);
            int maxX = screens.Max(s => s.Bounds.Right);
            int maxY = screens.Max(s => s.Bounds.Bottom);

            _offsetX = minX;
            _offsetY = minY;

            int totalWidth = maxX - minX;
            int totalHeight = maxY - minY;

            // Position window to cover ALL screens (including negative coordinates)
            Left = minX;
            Top = minY;
            Width = totalWidth;
            Height = totalHeight;

            // Debug output
            System.Diagnostics.Debug.WriteLine($"Screen Bounds: X={minX}, Y={minY}, Width={totalWidth}, Height={totalHeight}");
            foreach (var screen in screens)
            {
                System.Diagnostics.Debug.WriteLine($"  Screen: X={screen.Bounds.X}, Y={screen.Bounds.Y}, W={screen.Bounds.Width}, H={screen.Bounds.Height}, Primary={screen.Primary}");
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _startPoint = e.GetPosition(this);
                _isSelecting = true;

                if (_selectionRectangle == null)
                {
                    _selectionRectangle = new ShapesRectangle
                    {
                        Stroke = System.Windows.Media.Brushes.Red,
                        StrokeThickness = 3,
                        Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 0, 0))
                    };
                    SelectionCanvas.Children.Add(_selectionRectangle);
                }

                Canvas.SetLeft(_selectionRectangle, _startPoint.X);
                Canvas.SetTop(_selectionRectangle, _startPoint.Y);
                _selectionRectangle.Width = 0;
                _selectionRectangle.Height = 0;

                System.Diagnostics.Debug.WriteLine($"Mouse Down: {_startPoint}");
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelecting && _selectionRectangle != null)
            {
                var currentPoint = e.GetPosition(this);

                double x = Math.Min(_startPoint.X, currentPoint.X);
                double y = Math.Min(_startPoint.Y, currentPoint.Y);
                double width = Math.Abs(_startPoint.X - currentPoint.X);
                double height = Math.Abs(_startPoint.Y - currentPoint.Y);

                Canvas.SetLeft(_selectionRectangle, x);
                Canvas.SetTop(_selectionRectangle, y);
                _selectionRectangle.Width = width;
                _selectionRectangle.Height = height;
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting && _selectionRectangle != null)
            {
                _isSelecting = false;

                double x = Canvas.GetLeft(_selectionRectangle);
                double y = Canvas.GetTop(_selectionRectangle);
                double width = _selectionRectangle.Width;
                double height = _selectionRectangle.Height;

                if (width > 10 && height > 10)
                {
                    // Get DPI scaling factor
                    var source = PresentationSource.FromVisual(this);
                    double dpiScaleX = 1.0;
                    double dpiScaleY = 1.0;
                    
                    if (source != null)
                    {
                        dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                        dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                    }

                    // Convert WPF coordinates (DIP) to screen coordinates (pixels)
                    // Add window offset to get absolute screen coordinates
                    int screenX = (int)Math.Round(_offsetX + (x * dpiScaleX));
                    int screenY = (int)Math.Round(_offsetY + (y * dpiScaleY));
                    int pixelWidth = (int)Math.Round(width * dpiScaleX);
                    int pixelHeight = (int)Math.Round(height * dpiScaleY);

                    System.Diagnostics.Debug.WriteLine($"Selection: WPF=({x},{y},{width},{height}) Screen=({screenX},{screenY},{pixelWidth},{pixelHeight}) DPI=({dpiScaleX},{dpiScaleY}) Offset=({_offsetX},{_offsetY})");

                    SelectedRegion = new DrawingRectangle(screenX, screenY, pixelWidth, pixelHeight);
                    DialogResult = true;
                }
                else
                {
                    DialogResult = false;
                }

                Close();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}
