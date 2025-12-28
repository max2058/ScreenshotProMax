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
                // Stelle sicher, dass der Hotkey nicht bereits registriert ist
                if (_isPrintScreenRegistered)
                {
                    UnregisterPrintScreenHotkey();
                }

                // VK_SNAPSHOT = 0x2C (Print Screen)
                // MOD_NOREPEAT = 0x4000 verhindert wiederholte Nachrichten
                _isPrintScreenRegistered = NativeMethods.RegisterHotKey(_windowHandle, HOTKEY_ID_PRINT_SCREEN, 0x4000, 0x2C);
                
                if (_isPrintScreenRegistered)
                {
                    System.Diagnostics.Debug.WriteLine("Print Screen Hotkey erfolgreich registriert");
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    System.Diagnostics.Debug.WriteLine($"Fehler beim Registrieren des Print Screen Hotkeys: {error}");
                }
                
                return _isPrintScreenRegistered;
            }

            return false;
        }

        public void UnregisterPrintScreenHotkey()
        {
            if (_isPrintScreenRegistered && _windowHandle != IntPtr.Zero)
            {
                var result = NativeMethods.UnregisterHotKey(_windowHandle, HOTKEY_ID_PRINT_SCREEN);
                
                if (result)
                {
                    System.Diagnostics.Debug.WriteLine("Print Screen Hotkey erfolgreich deregistriert");
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    System.Diagnostics.Debug.WriteLine($"Fehler beim Deregistrieren des Print Screen Hotkeys: {error}");
                }
                
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
                    System.Diagnostics.Debug.WriteLine("Print Screen Hotkey erkannt - löse Event aus");
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
