using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows;

namespace ScreenshotProMax.Services;

public class ScreenshotService
{
    public Task<BitmapSource> CapturePrimaryScreenAsync()
    {
        return Task.Run(() =>
        {
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            using var bitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(screen.Bounds.X, screen.Bounds.Y, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
            }

            var handle = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                NativeMethods.DeleteObject(handle);
            }
        });
    }

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
