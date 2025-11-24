using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using ScreenshotProMax.Services;
using ScreenshotProMax.Views;
using Application = System.Windows.Application;

namespace ScreenshotProMax;

public partial class App : Application
{
    private NotifyIcon? _notifyIcon;
    private bool _isExit;
    private HotkeyService? _hotkeyService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        SetupTrayIcon();
        RegisterGlobalHotkey();
    }

    private void SetupTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
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
        // Create a hidden helper window just for hotkey registration if main window not yet created
        var helperWindow = new Window { Width = 0, Height = 0, ShowInTaskbar = false, WindowStyle = WindowStyle.ToolWindow, Visibility = Visibility.Hidden };
        helperWindow.Loaded += (_, _) =>
        {
            _hotkeyService = new HotkeyService();
            var handle = new WindowInteropHelper(helperWindow).Handle;
            if (_hotkeyService.RegisterHotkey(handle, ModifierKeys.Control, Key.D))
            {
                _hotkeyService.HotkeyPressed += (_, _) => TriggerRegionCapture();
            }
        };
        helperWindow.Show();
    }

    private async void TriggerRegionCapture()
    {
        // Ensure window minimized or hidden during capture
        if (Current.MainWindow is Window mw)
        {
            mw.WindowState = WindowState.Minimized;
        }

        var selector = new RegionSelectorWindow();
        selector.ShowDialog();
        if (selector.SelectedRegion.HasValue)
        {
            // Use ScreenshotService directly to avoid waiting for UI creation
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
            // Restore if cancelled
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
