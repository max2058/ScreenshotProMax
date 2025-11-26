using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DrawingRectangle = System.Drawing.Rectangle;

namespace ScreenshotProMax.Views
{
    public partial class WindowSelectorWindow : Window
    {
        private Rectangle? _highlightRect;
        private IntPtr _hoveredWindow = IntPtr.Zero;
        
        public DrawingRectangle? SelectedWindowRect { get; private set; }

        public WindowSelectorWindow()
        {
            InitializeComponent();
            Loaded += WindowSelectorWindow_Loaded;
        }

        private void WindowSelectorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var screens = System.Windows.Forms.Screen.AllScreens;
            if (screens.Length == 0)
            {
                Close();
                return;
            }

            // Calculate bounds across ALL screens
            int minX = screens.Min(s => s.Bounds.X);
            int minY = screens.Min(s => s.Bounds.Y);
            int maxX = screens.Max(s => s.Bounds.Right);
            int maxY = screens.Max(s => s.Bounds.Bottom);

            int totalWidth = maxX - minX;
            int totalHeight = maxY - minY;

            // Position window to cover ALL screens
            Left = minX;
            Top = minY;
            Width = totalWidth;
            Height = totalHeight;

            // Create highlight rectangle
            _highlightRect = new Rectangle
            {
                Stroke = Brushes.Red,
                StrokeThickness = 4,
                Fill = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0)),
                Visibility = Visibility.Collapsed
            };
            HighlightCanvas.Children.Add(_highlightRect);
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(this);
            var screens = System.Windows.Forms.Screen.AllScreens;
            
            int minX = screens.Min(s => s.Bounds.X);
            int minY = screens.Min(s => s.Bounds.Y);

            // Convert WPF coordinates to screen coordinates
            int screenX = (int)Math.Round(position.X + minX);
            int screenY = (int)Math.Round(position.Y + minY);

            // Get window at cursor position
            var point = new POINT { X = screenX, Y = screenY };
            IntPtr hwnd = WindowFromPoint(point);

            if (hwnd != IntPtr.Zero && hwnd != new System.Windows.Interop.WindowInteropHelper(this).Handle)
            {
                // Get the top-level window (not child controls)
                IntPtr rootWindow = GetAncestor(hwnd, 2); // GA_ROOT = 2
                if (rootWindow != IntPtr.Zero)
                {
                    hwnd = rootWindow;
                }

                if (hwnd != _hoveredWindow)
                {
                    _hoveredWindow = hwnd;
                    UpdateHighlight(minX, minY);
                }
            }
        }

        private void UpdateHighlight(int offsetX, int offsetY)
        {
            if (_highlightRect == null || _hoveredWindow == IntPtr.Zero)
                return;

            if (GetWindowRect(_hoveredWindow, out var rect))
            {
                // Convert screen coordinates to WPF coordinates
                double x = rect.Left - offsetX;
                double y = rect.Top - offsetY;
                double width = rect.Width;
                double height = rect.Height;

                System.Windows.Controls.Canvas.SetLeft(_highlightRect, x);
                System.Windows.Controls.Canvas.SetTop(_highlightRect, y);
                _highlightRect.Width = width;
                _highlightRect.Height = height;
                _highlightRect.Visibility = Visibility.Visible;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _hoveredWindow != IntPtr.Zero)
            {
                if (GetWindowRect(_hoveredWindow, out var rect))
                {
                    SelectedWindowRect = new DrawingRectangle(rect.Left, rect.Top, rect.Width, rect.Height);
                    DialogResult = true;
                    Close();
                }
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

        #region Native Methods

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        #endregion
    }
}
