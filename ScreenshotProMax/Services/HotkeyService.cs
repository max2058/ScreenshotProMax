using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace ScreenshotProMax.Services
{
    public class HotkeyService : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID_PRINT_SCREEN = 9001;

        private IntPtr _windowHandle;
        private HwndSource? _source;
        private bool _isPrintScreenRegistered;

        public event EventHandler? HotkeyPressed;

        public bool RegisterPrintScreenHotkey(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            if (_source == null)
            {
                _source = HwndSource.FromHwnd(_windowHandle);
                if (_source != null)
                {
                    _source.AddHook(HwndHook);
                }
            }

            if (_source != null)
            {
                // VK_SNAPSHOT = 0x2C (Print Screen)
                _isPrintScreenRegistered = NativeMethods.RegisterHotKey(_windowHandle, HOTKEY_ID_PRINT_SCREEN, 0, 0x2C);
                return _isPrintScreenRegistered;
            }

            return false;
        }

        public void UnregisterPrintScreenHotkey()
        {
            if (_isPrintScreenRegistered && _windowHandle != IntPtr.Zero)
            {
                NativeMethods.UnregisterHotKey(_windowHandle, HOTKEY_ID_PRINT_SCREEN);
                _isPrintScreenRegistered = false;
            }
        }

        public void UnregisterHotkey()
        {
            if (_source != null)
            {
                _source.RemoveHook(HwndHook);
                _source = null;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int hotkeyId = wParam.ToInt32();
                if (hotkeyId == HOTKEY_ID_PRINT_SCREEN)
                {
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterPrintScreenHotkey();
            UnregisterHotkey();
        }
    }
}
