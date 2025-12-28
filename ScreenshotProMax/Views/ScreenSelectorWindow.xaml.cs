using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenshotProMax.Views
{
    public partial class ScreenSelectorWindow : Window
    {
        private System.Windows.Forms.Screen? _hoveredScreen;

        public System.Windows.Forms.Screen? SelectedScreen { get; private set; }

        public ScreenSelectorWindow()
        {
            InitializeComponent();
            Loaded += ScreenSelectorWindow_Loaded;
        }

        private void ScreenSelectorWindow_Loaded(object sender, RoutedEventArgs e)
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

            // Draw screen outlines
            foreach (var screen in screens)
            {
                var rect = new Rectangle
                {
                    Width = screen.Bounds.Width,
                    Height = screen.Bounds.Height,
                    Stroke = Brushes.White,
                    StrokeThickness = 4,
                    Fill = Brushes.Transparent,
                    Tag = screen
                };

                Canvas.SetLeft(rect, screen.Bounds.X - minX);
                Canvas.SetTop(rect, screen.Bounds.Y - minY);
                ScreenCanvas.Children.Add(rect);

                // Add screen label
                var label = new TextBlock
                {
                    Text = screen.Primary ? "Primärer Bildschirm" : $"Bildschirm {screens.ToList().IndexOf(screen) + 1}",
                    Foreground = Brushes.White,
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                    Padding = new Thickness(10),
                    Tag = screen
                };

                double labelX = screen.Bounds.X - minX + (screen.Bounds.Width - 300) / 2;
                double labelY = screen.Bounds.Y - minY + (screen.Bounds.Height - 40) / 2;
                
                Canvas.SetLeft(label, labelX);
                Canvas.SetTop(label, labelY);
                ScreenCanvas.Children.Add(label);
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(this);
            var screens = System.Windows.Forms.Screen.AllScreens;
            
            int minX = screens.Min(s => s.Bounds.X);
            int minY = screens.Min(s => s.Bounds.Y);

            // Convert WPF coordinates to screen coordinates
            double screenX = position.X + minX;
            double screenY = position.Y + minY;

            System.Windows.Forms.Screen? newHoveredScreen = null;
            foreach (var screen in screens)
            {
                if (screenX >= screen.Bounds.X && screenX < screen.Bounds.Right &&
                    screenY >= screen.Bounds.Y && screenY < screen.Bounds.Bottom)
                {
                    newHoveredScreen = screen;
                    break;
                }
            }

            if (newHoveredScreen != _hoveredScreen)
            {
                _hoveredScreen = newHoveredScreen;
                UpdateHighlight();
            }
        }

        private void UpdateHighlight()
        {
            foreach (var child in ScreenCanvas.Children)
            {
                if (child is Rectangle rect && rect.Tag is System.Windows.Forms.Screen screen)
                {
                    if (screen == _hoveredScreen)
                    {
                        rect.Fill = new SolidColorBrush(Color.FromArgb(80, 0, 120, 215));
                        rect.Stroke = Brushes.Cyan;
                        rect.StrokeThickness = 6;
                    }
                    else
                    {
                        rect.Fill = Brushes.Transparent;
                        rect.Stroke = Brushes.White;
                        rect.StrokeThickness = 4;
                    }
                }
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _hoveredScreen != null)
            {
                SelectedScreen = _hoveredScreen;
                DialogResult = true;
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
