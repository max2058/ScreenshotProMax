using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace ScreenshotProMax.Services
{
    public class HotkeyService : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;

        private IntPtr _windowHandle;
        private HwndSource? _source;
        private bool _isRegistered;

        public event EventHandler? HotkeyPressed;

        public bool RegisterHotkey(IntPtr windowHandle, ModifierKeys modifiers, Key key)
        {
            _windowHandle = windowHandle;
            _source = HwndSource.FromHwnd(_windowHandle);

            if (_source != null)
            {
                _source.AddHook(HwndHook);
                
                uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
                uint mod = (uint)modifiers;

                _isRegistered = NativeMethods.RegisterHotKey(_windowHandle, HOTKEY_ID, mod, vk);
                return _isRegistered;
            }

            return false;
        }

        public void UnregisterHotkey()
        {
            if (_isRegistered && _windowHandle != IntPtr.Zero)
            {
                NativeMethods.UnregisterHotKey(_windowHandle, HOTKEY_ID);
                _isRegistered = false;
            }

            if (_source != null)
            {
                _source.RemoveHook(HwndHook);
                _source = null;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                handled = true;
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterHotkey();
        }
    }
}
