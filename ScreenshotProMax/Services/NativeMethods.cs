using System;
using System.Runtime.InteropServices;

namespace ScreenshotProMax.Services
{
    internal static class NativeMethods
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        // Zusätzliche Windows-API-Funktionen für Registry-Manipulation
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetEnvironmentVariable(string lpName, string lpValue);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        // Hotkey-Modifier-Konstanten
        public const uint MOD_NONE = 0x0000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        // Virtual Key Codes
        public const uint VK_SNAPSHOT = 0x2C; // Print Screen
        public const uint VK_LWIN = 0x5B;     // Left Windows key
        public const uint VK_RWIN = 0x5C;     // Right Windows key

        // SystemParametersInfo-Konstanten
        public const uint SPI_SETSCREENSAVETIMEOUT = 0x000F;
        public const uint SPIF_UPDATEINIFILE = 0x0001;
        public const uint SPIF_SENDCHANGE = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }
    }
}
