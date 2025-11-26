using System;
using System.Collections.Generic;
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
        private System.Windows.Threading.DispatcherTimer? _mouseTimer;
        private bool _isClosing = false;
        
        public DrawingRectangle? SelectedWindowRect { get; private set; }

        public WindowSelectorWindow()
        {
            InitializeComponent();
            Loaded += WindowSelectorWindow_Loaded;
        }

        private void WindowSelectorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set instruction text
            InstructionText.Text = "Klicken Sie auf ein Fenster, um es auszuwählen • ESC zum Abbrechen";

            var screens = System.Windows.Forms.Screen.AllScreens;
            if (screens.Length == 0)
            {
                CloseWindow(false);
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
                Stroke = new SolidColorBrush(Color.FromRgb(255, 69, 0)),
                StrokeThickness = 4,
                Fill = Brushes.Transparent,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
            HighlightCanvas.Children.Add(_highlightRect);

            // Ensure window is focused and topmost
            Activate();
            Focus();
            Topmost = true;

            // Start a timer to poll mouse position
            _mouseTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _mouseTimer.Tick += MouseTimer_Tick;
            _mouseTimer.Start();
        }

        private void MouseTimer_Tick(object? sender, EventArgs e)
        {
            if (_isClosing) return;

            if (GetCursorPos(out var cursorPos))
            {
                UpdateWindowHighlight(cursorPos.X, cursorPos.Y);
            }
        }

        private void UpdateWindowHighlight(int screenX, int screenY)
        {
            var screens = System.Windows.Forms.Screen.AllScreens;
            int minX = screens.Min(s => s.Bounds.X);
            int minY = screens.Min(s => s.Bounds.Y);

            // Find window at cursor position by enumerating all windows
            IntPtr foundWindow = FindWindowAtPoint(screenX, screenY);

            if (foundWindow != IntPtr.Zero && foundWindow != _hoveredWindow)
            {
                _hoveredWindow = foundWindow;
                UpdateHighlight(minX, minY);
            }
            else if (foundWindow == IntPtr.Zero && _highlightRect != null)
            {
                _highlightRect.Visibility = Visibility.Collapsed;
                _hoveredWindow = IntPtr.Zero;
            }
        }

        private IntPtr FindWindowAtPoint(int x, int y)
        {
            IntPtr bestWindow = IntPtr.Zero;
            int bestArea = 0;

            // Enumerate all top-level windows
            EnumWindows((hwnd, lParam) =>
            {
                // Skip if not visible
                if (!IsWindowVisible(hwnd))
                    return true;

                // Skip if minimized
                if (IsIconic(hwnd))
                    return true;

                // Get window rectangle
                if (!GetWindowRect(hwnd, out var rect))
                    return true;

                // Skip if window is too small (likely not a real application window)
                if (rect.Width < 50 || rect.Height < 50)
                    return true;

                // Check if point is inside this window
                if (x >= rect.Left && x <= rect.Right && y >= rect.Top && y <= rect.Bottom)
                {
                    int area = rect.Width * rect.Height;
                    
                    // Find the smallest window that contains the point
                    if (bestWindow == IntPtr.Zero || area < bestArea)
                    {
                        bestWindow = hwnd;
                        bestArea = area;
                    }
                }

                return true;
            }, IntPtr.Zero);

            return bestWindow;
        }

        private void UpdateHighlight(int offsetX, int offsetY)
        {
            if (_highlightRect == null || _hoveredWindow == IntPtr.Zero)
                return;

            if (GetWindowRect(_hoveredWindow, out var rect))
            {
                if (rect.Width <= 0 || rect.Height <= 0)
                {
                    _highlightRect.Visibility = Visibility.Collapsed;
                    return;
                }

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
            else
            {
                _highlightRect.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Mouse clicked! Left: {e.LeftButton}, Right: {e.RightButton}, Hovered: {_hoveredWindow}");
            
            if (_isClosing) return;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (_hoveredWindow != IntPtr.Zero)
                {
                    System.Diagnostics.Debug.WriteLine($"Capturing window: {_hoveredWindow}");
                    
                    if (GetWindowRect(_hoveredWindow, out var rect))
                    {
                        if (rect.Width > 0 && rect.Height > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"Window rect: ({rect.Left}, {rect.Top}, {rect.Width}, {rect.Height})");
                            SelectedWindowRect = new DrawingRectangle(rect.Left, rect.Top, rect.Width, rect.Height);
                            CloseWindow(true);
                            return;
                        }
                    }
                }
                
                // If no window was hovered, still close to prevent hanging
                System.Diagnostics.Debug.WriteLine("No valid window to capture, closing anyway");
                CloseWindow(false);
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                System.Diagnostics.Debug.WriteLine("Right click - cancelling");
                CloseWindow(false);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && !_isClosing)
            {
                CloseWindow(false);
            }
        }

        private void CloseWindow(bool success)
        {
            if (_isClosing) return;
            
            _isClosing = true;
            _mouseTimer?.Stop();
            DialogResult = success;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _mouseTimer?.Stop();
            base.OnClosed(e);
        }

        #region Native Methods

        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

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
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        #endregion
    }
}
