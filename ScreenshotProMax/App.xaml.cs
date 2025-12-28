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
    private HotkeyService? _hotkeyService;
    private GlobalKeyboardHook? _globalKeyboardHook;
    private HwndSource? _hwndSource;
    private SettingsService? _settingsService;
    public static LocalizedStrings Loc { get; private set; } = new LocalizedStrings();

    protected override void OnStartup(StartupEventArgs e)
    {
        // Prevent WPF from shutting down when windows close; we'll control shutdown explicitly
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _settingsService = new SettingsService();

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

        // Check if this is the first startup
        if (!_settingsService.GetIsInitialSetupCompleted())
        {
            ShowInitialSetup();
        }

        // **NEUE FUNKTIONALITÄT**: Snipping Tool Startup-Check
        PerformStartupSnippingToolCheck();

        SetupTrayIcon();
        InitializeScreenshotCapture();
    }

    /// <summary>
    /// **NEUE METHODE**: Überprüft beim Anwendungsstart Snipping Tool Prozesse
    /// </summary>
    private void PerformStartupSnippingToolCheck()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== Startup Snipping Tool Check ===");
            _settingsService?.CheckAndTerminateSnippingToolOnStartup();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Startup Snipping Tool Check: {ex.Message}");
        }
    }

    private void ShowInitialSetup()
    {
        var setupWindow = new InitialSetupWindow();
        var result = setupWindow.ShowDialog();
        
        // If user completed the setup, update culture and theme if they were changed
        if (result == true)
        {
            // Reload culture in case it was changed during setup
            var culture = Settings.Default.CultureLang;
            if (!string.IsNullOrEmpty(culture))
            {
                var ci = new CultureInfo(culture);
                CultureInfo.DefaultThreadCurrentUICulture = ci;
                CultureInfo.DefaultThreadCurrentCulture = ci;
                LanguageGUI.Culture = ci;
                
                // Update localized strings
                App.Loc.Refresh();
            }

            // Reload theme in case it was changed during setup
            try
            {
                var theme = Settings.Default.MainDesignStyle ?? "Dark";
                // Apply theme to all windows using ThemeManager
                ControlzEx.Theming.ThemeManager.Current.ChangeThemeBaseColor(Application.Current, theme);
            }
            catch (Exception ex)
            {
                // If theme update fails, log but continue
                System.Diagnostics.Debug.WriteLine($"Failed to update theme after setup: {ex.Message}");
            }
        }
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
        menu.Items.Add("Screenshot Region (Drucktaste)", null, (_, _) => TriggerRegionCapture());
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
        
        // Cleanup screenshot capture systems
        CleanupScreenshotCapture();
        
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        Current.Shutdown();
    }

    /// <summary>
    /// Initialisiert das Screenshot-Capture-System basierend auf den Einstellungen
    /// </summary>
    private void InitializeScreenshotCapture()
    {
        try
        {
            bool isDefaultApp = _settingsService?.GetIsDefaultScreenshotApp() ?? false;
            
            if (isDefaultApp)
            {
                System.Diagnostics.Debug.WriteLine("Initialisiere als Standard-Screenshot-App");
                SetupGlobalKeyboardHook();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Initialisiere mit Standard-Hotkey-Service");
                SetupStandardHotkey();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Initialisieren des Screenshot-Capture-Systems: {ex.Message}");
            // Fallback auf Standard-Hotkey
            SetupStandardHotkey();
        }
    }

    /// <summary>
    /// Einrichtung des Global Keyboard Hooks für vollständige Print Screen-Kontrolle
    /// </summary>
    private void SetupGlobalKeyboardHook()
    {
        try
        {
            _globalKeyboardHook = new GlobalKeyboardHook();
            _globalKeyboardHook.PrintScreenPressed += OnPrintScreenPressed;
            
            if (_globalKeyboardHook.InstallHook())
            {
                System.Diagnostics.Debug.WriteLine("Global Keyboard Hook erfolgreich installiert");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Fehler beim Installieren des Global Keyboard Hooks - Fallback auf Standard-Hotkey");
                _globalKeyboardHook?.Dispose();
                _globalKeyboardHook = null;
                SetupStandardHotkey();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ausnahme beim Setup des Global Keyboard Hooks: {ex.Message}");
            _globalKeyboardHook?.Dispose();
            _globalKeyboardHook = null;
            SetupStandardHotkey();
        }
    }

    /// <summary>
    /// Einrichtung des Standard-Hotkey-Services (Fallback)
    /// </summary>
    private void SetupStandardHotkey()
    {
        try
        {
            // Create a hidden message window for standard hotkey
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
            
            if (_hotkeyService.RegisterPrintScreenHotkey(handle))
            {
                _hotkeyService.HotkeyPressed += OnPrintScreenPressed;
                System.Diagnostics.Debug.WriteLine("Standard Print Screen Hotkey registriert");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Fehler beim Registrieren des Standard Print Screen Hotkeys");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Setup des Standard-Hotkey-Services: {ex.Message}");
        }
    }

    /// <summary>
    /// **ERWEITERTE METHODE**: Event-Handler für Print Screen-Taste mit aggressiver Snipping Tool Unterdrückung
    /// </summary>
    private async void OnPrintScreenPressed(object? sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== Print Screen-Event empfangen ===");
            
            // **NEUE FUNKTIONALITÄT**: Aggressive Snipping Tool Unterdrückung
            if (_settingsService != null)
            {
                // Starte aggressive Unterdrückung parallel zur Screenshot-Aufnahme
                var suppressionTask = _settingsService.AggressiveSnippingToolSuppressionAsync();
                
                // Starte Screenshot-Aufnahme
                var screenshotTask = TriggerRegionCapture();
                
                // Warte auf beide Tasks
                await System.Threading.Tasks.Task.WhenAll(suppressionTask, screenshotTask);
            }
            else
            {
                // Fallback: nur Screenshot
                await TriggerRegionCapture();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Verarbeiten des Print Screen-Events: {ex.Message}");
        }
    }

    /// <summary>
    /// Aktualisiert das Screenshot-Capture-System basierend auf den neuen Einstellungen
    /// </summary>
    /// <param name="enableAsDefault">True für Global Hook, False für Standard Hotkey</param>
    public void UpdateScreenshotCaptureSystem(bool enableAsDefault)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Aktualisiere Screenshot-Capture-System: {(enableAsDefault ? "Global Hook" : "Standard Hotkey")}");
            
            // Cleanup existing systems
            CleanupScreenshotCapture();
            
            // Setup new system
            if (enableAsDefault)
            {
                SetupGlobalKeyboardHook();
                
                // **NEUE FUNKTIONALITÄT**: Sofort Snipping Tool Prozesse prüfen und beenden
                System.Threading.Tasks.Task.Run(() =>
                {
                    if (SnippingToolProcessManager.IsSnippingToolRunning())
                    {
                        var terminated = SnippingToolProcessManager.TerminateSnippingToolProcesses();
                        System.Diagnostics.Debug.WriteLine($"Bei Aktivierung der Standard-App {terminated} Snipping Tool Prozesse beendet");
                    }
                });
            }
            else
            {
                SetupStandardHotkey();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Aktualisieren des Screenshot-Capture-Systems: {ex.Message}");
        }
    }

    /// <summary>
    /// Legacy-Methode für Rückwärtskompatibilität
    /// </summary>
    public void UpdatePrintScreenHotkey(bool enable)
    {
        UpdateScreenshotCaptureSystem(enable);
    }

    /// <summary>
    /// Bereinigt alle Screenshot-Capture-Systeme
    /// </summary>
    private void CleanupScreenshotCapture()
    {
        try
        {
            // Cleanup Global Keyboard Hook
            if (_globalKeyboardHook != null)
            {
                _globalKeyboardHook.PrintScreenPressed -= OnPrintScreenPressed;
                _globalKeyboardHook.Dispose();
                _globalKeyboardHook = null;
                System.Diagnostics.Debug.WriteLine("Global Keyboard Hook bereinigt");
            }

            // Cleanup Standard Hotkey Service
            if (_hotkeyService != null)
            {
                _hotkeyService.HotkeyPressed -= OnPrintScreenPressed;
                _hotkeyService.Dispose();
                _hotkeyService = null;
                System.Diagnostics.Debug.WriteLine("Standard Hotkey Service bereinigt");
            }

            // Cleanup HwndSource
            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource.Dispose();
                _hwndSource = null;
                System.Diagnostics.Debug.WriteLine("HwndSource bereinigt");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Bereinigen der Screenshot-Capture-Systeme: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task TriggerRegionCapture()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("TriggerRegionCapture gestartet");
            
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
                
                System.Diagnostics.Debug.WriteLine("Screenshot erfolgreich aufgenommen und in MainWindow geladen");
            }
            else
            {
                ShowMainWindow();
                System.Diagnostics.Debug.WriteLine("Screenshot-Aufnahme abgebrochen");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler bei TriggerRegionCapture: {ex.Message}");
            ShowMainWindow();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        CleanupScreenshotCapture();
        
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        
        base.OnExit(e);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // Forward to HotkeyService window message processing if needed
        return IntPtr.Zero;
    }
}
