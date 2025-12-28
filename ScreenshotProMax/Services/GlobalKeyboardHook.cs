using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenshotProMax.Services
{
    /// <summary>
    /// Implementiert einen globalen Tastatur-Hook zum Abfangen von Tastaturereignissen
    /// </summary>
    public class GlobalKeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private LowLevelKeyboardProc _hookProc;
        private IntPtr _hookId = IntPtr.Zero;
        private bool _disposed = false;

        /// <summary>
        /// Ereignis wird ausgelöst, wenn die Print Screen-Taste gedrückt wird
        /// </summary>
        public event EventHandler? PrintScreenPressed;

        /// <summary>
        /// Ereignis wird ausgelöst, wenn eine Taste gedrückt wird (für Debugging)
        /// </summary>
        public event EventHandler<KeyPressedEventArgs>? KeyPressed;

        public GlobalKeyboardHook()
        {
            _hookProc = HookCallback;
        }

        /// <summary>
        /// Installiert den globalen Tastatur-Hook
        /// </summary>
        /// <returns>True wenn erfolgreich installiert</returns>
        public bool InstallHook()
        {
            if (_hookId != IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("Hook ist bereits installiert");
                return true;
            }

            try
            {
                _hookId = SetHook(_hookProc);
                bool success = _hookId != IntPtr.Zero;
                
                System.Diagnostics.Debug.WriteLine(success 
                    ? "Global Keyboard Hook erfolgreich installiert" 
                    : "Fehler beim Installieren des Global Keyboard Hooks");
                
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ausnahme beim Installieren des Hooks: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deinstalliert den globalen Tastatur-Hook
        /// </summary>
        /// <returns>True wenn erfolgreich deinstalliert</returns>
        public bool UninstallHook()
        {
            if (_hookId == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("Kein Hook zu deinstallieren");
                return true;
            }

            try
            {
                bool success = UnhookWindowsHookEx(_hookId);
                if (success)
                {
                    _hookId = IntPtr.Zero;
                    System.Diagnostics.Debug.WriteLine("Global Keyboard Hook erfolgreich deinstalliert");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Fehler beim Deinstallieren des Global Keyboard Hooks");
                }
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ausnahme beim Deinstallieren des Hooks: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Installiert den Low-Level Tastatur-Hook
        /// </summary>
        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                if (curModule?.ModuleName == null)
                    return IntPtr.Zero;

                IntPtr moduleHandle = GetModuleHandle(curModule.ModuleName);
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    moduleHandle, 0);
            }
        }

        /// <summary>
        /// Hook-Callback-Funktion die bei jeder Tastatureingabe aufgerufen wird
        /// </summary>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0)
                {
                    // Nur auf Key Down Events reagieren (WM_KEYDOWN = 0x0100, WM_SYSKEYDOWN = 0x0104)
                    if (wParam == (IntPtr)0x0100 || wParam == (IntPtr)0x0104)
                    {
                        int vkCode = Marshal.ReadInt32(lParam);
                        Keys key = (Keys)vkCode;

                        // Debug-Ausgabe für alle Tasten (optional)
                        KeyPressed?.Invoke(this, new KeyPressedEventArgs(key));

                        // Spezielle Behandlung für Print Screen (VK_SNAPSHOT = 0x2C = 44)
                        if (vkCode == 0x2C) // VK_SNAPSHOT
                        {
                            System.Diagnostics.Debug.WriteLine("Print Screen-Taste erkannt durch Global Hook");
                            
                            // Ereignis auslösen
                            PrintScreenPressed?.Invoke(this, EventArgs.Empty);
                            
                            // Tastaturereignis unterdrücken (nicht an andere Anwendungen weiterleiten)
                            return (IntPtr)1;
                        }

                        // Optionally handle other screenshot-related combinations
                        // Win + Shift + S would require more complex logic to detect key combinations
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler in Hook-Callback: {ex.Message}");
            }

            // Für alle anderen Tasten: normal weiterleiten
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        /// <summary>
        /// Überprüft ob der Hook installiert ist
        /// </summary>
        public bool IsHookInstalled => _hookId != IntPtr.Zero;

        #region P/Invoke Declarations

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Managed resources cleanup
                }

                // Unmanaged resources cleanup
                UninstallHook();
                _disposed = true;
            }
        }

        ~GlobalKeyboardHook()
        {
            Dispose(false);
        }

        #endregion
    }

    /// <summary>
    /// Event-Args für Key-Press-Events
    /// </summary>
    public class KeyPressedEventArgs : EventArgs
    {
        public Keys Key { get; }

        public KeyPressedEventArgs(Keys key)
        {
            Key = key;
        }
    }
}