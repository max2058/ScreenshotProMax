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
    private HotkeyService? _hotkeyService;
    public static LocalizedStrings Loc { get; private set; } = new LocalizedStrings();

    protected override void OnStartup(StartupEventArgs e)
    {
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
        var helperWindow = new Window { Width = 0, Height = 0, ShowInTaskbar = false, WindowStyle = WindowStyle.ToolWindow, Visibility = Visibility.Hidden };
        helperWindow.Loaded += (_, _) =>
        {
            _hotkey_service_Register(helperWindow);
        };
        helperWindow.Show();
    }

    private void _hotkey_service_Register(Window helperWindow)
    {
        _hotkeyService = new HotkeyService();
        var handle = new WindowInteropHelper(helperWindow).Handle;
        if (_hotkeyService.RegisterHotkey(handle, ModifierKeys.Control, Key.D))
        {
            _hotkeyService.HotkeyPressed += (_, _) => TriggerRegionCapture();
        }
    }

    private async void TriggerRegionCapture()
    {
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
        base.OnExit(e);
    }
}
