using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ScreenshotProMax.Services
{
    public enum CaptureMode
    {
        FullScreen,
        ActiveWindow,
        Region
    }

    public class ScreenshotService
    {
        public Task<BitmapSource> CapturePrimaryScreenAsync()
        {
            return CaptureScreenAsync(System.Windows.Forms.Screen.PrimaryScreen);
        }

        public Task<BitmapSource> CaptureAllScreensAsync()
        {
            return Task.Run(() =>
            {
                var screens = System.Windows.Forms.Screen.AllScreens;
                if (screens.Length == 0)
                    throw new InvalidOperationException("No screens found.");

                // Calculate total bounds
                int minX = screens.Min(s => s.Bounds.X);
                int minY = screens.Min(s => s.Bounds.Y);
                int maxX = screens.Max(s => s.Bounds.X + s.Bounds.Width);
                int maxY = screens.Max(s => s.Bounds.Y + s.Bounds.Height);

                int width = maxX - minX;
                int height = maxY - minY;

                using var bitmap = new Bitmap(width, height);
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(minX, minY, 0, 0, new System.Drawing.Size(width, height), System.Drawing.CopyPixelOperation.SourceCopy);
                }

                return ConvertBitmapToBitmapSource(bitmap);
            });
        }

        public Task<BitmapSource> CaptureActiveWindowAsync()
        {
            return Task.Run(() =>
            {
                IntPtr hwnd = NativeMethods.GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                    throw new InvalidOperationException("No active window found.");

                if (!NativeMethods.GetWindowRect(hwnd, out var rect))
                    throw new InvalidOperationException("Could not get window bounds.");

                using var bitmap = new Bitmap(rect.Width, rect.Height);
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(rect.Width, rect.Height), System.Drawing.CopyPixelOperation.SourceCopy);
                }

                return ConvertBitmapToBitmapSource(bitmap);
            });
        }

        public Task<BitmapSource> CaptureRegionAsync(System.Drawing.Rectangle region)
        {
            return Task.Run(() =>
            {
                if (region.Width <= 0 || region.Height <= 0)
                    throw new ArgumentException("Invalid region dimensions.");

                using var bitmap = new Bitmap(region.Width, region.Height);
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(region.X, region.Y, 0, 0, region.Size, System.Drawing.CopyPixelOperation.SourceCopy);
                }

                return ConvertBitmapToBitmapSource(bitmap);
            });
        }

        public Task<BitmapSource> CaptureScreenAsync(System.Windows.Forms.Screen? screen)
        {
            return Task.Run(() =>
            {
                if (screen == null)
                    throw new InvalidOperationException("No screen provided.");

                using var bitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(screen.Bounds.X, screen.Bounds.Y, 0, 0, bitmap.Size, System.Drawing.CopyPixelOperation.SourceCopy);
                }

                return ConvertBitmapToBitmapSource(bitmap);
            });
        }

        private BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            var handle = bitmap.GetHbitmap();
            try
            {
                var bmpSource = Imaging.CreateBitmapSourceFromHBitmap(
                    handle,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                // Freeze to make it thread-safe for the UI thread
                if (bmpSource.CanFreeze)
                    bmpSource.Freeze();

                return bmpSource;
            }
            finally
            {
                NativeMethods.DeleteObject(handle);
            }
        }
    }
}
