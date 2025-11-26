using System;
using ScreenshotProMax.Services;
using ScreenshotProMax.Views;
using ScreenshotProMax.Localization;
using ScreenshotProMax.Properties;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using Application = System.Windows.Application;
using System.Globalization;
using ScreenshotProMax.Resources;

namespace ScreenshotProMax;

public partial class App : Application
{
    private NotifyIcon? _notifyIcon;
    private bool _isExit;
    private HotkeyService? _hotkey_service;
    private HotkeyService? _hotkeyService;
    private HwndSource? _hwndSource;
    public static LocalizedStrings Loc { get; private set; } = new LocalizedStrings();

    protected override void OnStartup(StartupEventArgs e)
    {
        // Prevent WPF from shutting down when windows close; we'll control shutdown explicitly
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Set culture from saved settings
        var culture = Settings.Default.CultureLang;
        if (!string.IsNullOrEmpty(culture))
        {
            var ci = new CultureInfo(culture);
            CultureInfo.DefaultThreadCurrentUICulture = ci;
            CultureInfo.DefaultThreadCurrentCulture = ci;
            LanguageGUI.Culture = ci; // set resource culture
        }

        // Apply saved theme
        var theme = Settings.Default.MainDesignStyle ?? "Dark";
        var baseUri = $"pack://application:,,,/MahApps.Metro;component/Styles/Themes/{theme}.Steel.xaml";
        Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new System.Uri(baseUri) });

        base.OnStartup(e);

        SetupTrayIcon();
        RegisterGlobalHotkey();
    }

    private void SetupTrayIcon()
    {
        System.Drawing.Icon? trayIcon = null;
        try
        {
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            trayIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
        }
        catch
        {
            trayIcon = null;
        }

        _notifyIcon = new NotifyIcon
        {
            Icon = trayIcon ?? System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "ScreenshotProMax"
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Screenshot Region (Strg+D)", null, (_, _) => TriggerRegionCapture());
        menu.Items.Add("Öffnen", null, (_, _) => ShowMainWindow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Beenden", null, (_, _) => ExitApplication());
        _notifyIcon.ContextMenuStrip = menu;

        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (_isExit) return;
        if (Current.MainWindow is MainWindow existing)
        {
            existing.Show();
            existing.WindowState = WindowState.Normal;
            existing.Activate();
        }
        else
        {
            var window = new MainWindow();
            Current.MainWindow = window;
            window.Show();
        }
    }

    private void ExitApplication()
    {
        _isExit = true;
        _hotkeyService?.Dispose();
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        Current.Shutdown();
    }

    private void RegisterGlobalHotkey()
    {
        // Create a hidden message window (HwndSource) to register a global hotkey without showing any WPF Window
        var parameters = new HwndSourceParameters("HotkeyMessageWindow")
        {
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
            ParentWindow = IntPtr.Zero,
            WindowStyle = unchecked((int)0x80000000) // WS_POPUP
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);

        _hotkeyService = new HotkeyService();
        var handle = _hwndSource.Handle;
        if (_hotkeyService.RegisterHotkey(handle, ModifierKeys.Control, Key.D))
        {
            _hotkeyService.HotkeyPressed += (_, _) => TriggerRegionCapture();
        }
    }

    private async void TriggerRegionCapture()
    {
        // Minimize main window if visible
        if (Current.MainWindow is Window mw)
        {
            mw.WindowState = WindowState.Minimized;
        }

        var selector = new RegionSelectorWindow();
        selector.ShowDialog();
        if (selector.SelectedRegion.HasValue)
        {
            var service = new ScreenshotService();
            var bmp = await service.CaptureRegionAsync(selector.SelectedRegion.Value);
            ShowMainWindow();
            if (Current.MainWindow is Views.MainWindow mainW && mainW.DataContext is ViewModels.MainViewModel vm)
            {
                vm.CapturedImage = bmp;
                vm.ResetAnnotations();
            }
        }
        else
        {
            ShowMainWindow();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Dispose();
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
        }
        base.OnExit(e);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // Forward to HotkeyService window message processing if needed
        return IntPtr.Zero;
    }
}
