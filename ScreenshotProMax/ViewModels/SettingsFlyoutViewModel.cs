using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlzEx.Theming;
using ScreenshotProMax.Interfaces;
using ScreenshotProMax.Services;
using ScreenshotProMax.Resources;
using ScreenshotProMax;
using System.Threading.Tasks;
using System;

namespace ScreenshotProMax.ViewModels
{
    public partial class SettingsFlyoutViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;

        public SettingsFlyoutViewModel()
        {
            _settingsService = new SettingsService();

            LoadSupportedCultures();

            // Lade aktuellen Theme-Status
            IsDarkTheme = _settingsService.GetAppBaseTheme()?.Equals("Dark", System.StringComparison.OrdinalIgnoreCase) == true;

            AppVersion = _settings_service_AppVersion();

            // Lade aktuelle Sprache
            SelectedCulture = _settingsService.GetAppLanguage();

            // Lade Standard-Screenshot-App Einstellung
            IsDefaultScreenshotApp = _settingsService.GetIsDefaultScreenshotApp();

            // Prüfe Administrator-Rechte
            IsRunningAsAdministrator = ((SettingsService)_settingsService).IsAdministratorRightsAvailable();

            // Initialer Status
            UpdateDefaultAppStatus();

            // Reagiere auf Property-Änderungen
            PropertyChanged += SettingsFlyoutViewModel_PropertyChanged;
        }

        [ObservableProperty]
        private ObservableCollection<CultureInfo> supportedCultures = new();

        [ObservableProperty]
        private CultureInfo? selectedCulture;

        [ObservableProperty]
        private bool isDarkTheme;

        [ObservableProperty]
        private bool isDefaultScreenshotApp;

        [ObservableProperty]
        private string appVersion = string.Empty;

        [ObservableProperty]
        private bool isRunningAsAdministrator;

        [ObservableProperty]
        private bool isProcessingDefaultAppChange;

        [ObservableProperty]
        private string defaultAppStatus = "Inaktiv";

        [ObservableProperty]
        private string processMonitoringStatus = "Inaktiv";

        private void LoadSupportedCultures()
        {
            SupportedCultures = new ObservableCollection<CultureInfo>
            {
                new CultureInfo("de-DE"),
                new CultureInfo("en-US"),
                new CultureInfo("pl-PL")
            };
        }

        private async void SettingsFlyoutViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsDarkTheme))
            {
                var theme = IsDarkTheme ? "Dark" : "Light";
                ThemeManager.Current.ChangeThemeBaseColor(Application.Current, theme);
                _settingsService.SetAppBaseTheme(theme);
            }
            else if (e.PropertyName == nameof(IsDefaultScreenshotApp))
            {
                await HandleDefaultScreenshotAppChange();
            }
        }

        /// <summary>
        /// Aktualisiert den Status der Standard-App und Prozess-Überwachung
        /// </summary>
        private void UpdateDefaultAppStatus()
        {
            if (IsDefaultScreenshotApp)
            {
                DefaultAppStatus = "? Aktiv";
                
                // Prüfe ob Snipping Tool Prozesse laufen
                var isSnippingToolRunning = SnippingToolProcessManager.IsSnippingToolRunning();
                ProcessMonitoringStatus = isSnippingToolRunning 
                    ? "? Snipping Tool erkannt" 
                    : "? Überwacht";
            }
            else
            {
                DefaultAppStatus = "Inaktiv";
                ProcessMonitoringStatus = "Inaktiv";
            }
        }

        /// <summary>
        /// Behandelt die Änderung der Standard-Screenshot-App-Einstellung
        /// </summary>
        private async Task HandleDefaultScreenshotAppChange()
        {
            if (IsProcessingDefaultAppChange) return;

            IsProcessingDefaultAppChange = true;
            DefaultAppStatus = "Verarbeitung...";
            ProcessMonitoringStatus = "Verarbeitung...";

            try
            {
                // Führe die Änderung in einem separaten Task aus, um UI-Blockierung zu vermeiden
                var success = await Task.Run(() => _settingsService.SetIsDefaultScreenshotApp(IsDefaultScreenshotApp));

                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"Standard-Screenshot-App Einstellung erfolgreich auf {IsDefaultScreenshotApp} gesetzt");
                    
                    // Aktualisiere das Screenshot-Capture-System in der Hauptanwendung
                    if (Application.Current is App app)
                    {
                        app.UpdateScreenshotCaptureSystem(IsDefaultScreenshotApp);
                    }
                    
                    // Aktualisiere den Status
                    UpdateDefaultAppStatus();
                    
                    // Zusätzliche Prozess-Prüfung nach der Änderung
                    if (IsDefaultScreenshotApp)
                    {
                        await Task.Delay(1000); // Kurze Verzögerung
                        var remainingProcesses = await Task.Run(() =>
                        {
                            if (SnippingToolProcessManager.IsSnippingToolRunning())
                            {
                                return SnippingToolProcessManager.TerminateSnippingToolProcesses();
                            }
                            return 0;
                        });
                        
                        if (remainingProcesses > 0)
                        {
                            ProcessMonitoringStatus = $"? {remainingProcesses} Prozesse beendet";
                        }
                    }
                    
                    // Zeige ausführliche Bestätigungsnachricht
                    var message = CreateStatusMessage(IsDefaultScreenshotApp, true);
                    var title = IsDefaultScreenshotApp ? "Standard-Screenshot-App aktiviert" : "Standard-Screenshot-App deaktiviert";
                    
                    MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Bei Fehler die Einstellung zurücksetzen
                    var previousValue = IsDefaultScreenshotApp;
                    IsDefaultScreenshotApp = !IsDefaultScreenshotApp;
                    UpdateDefaultAppStatus();
                    
                    var errorMessage = CreateErrorMessage(previousValue);
                    MessageBox.Show(errorMessage, "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // Bei Ausnahme die Einstellung zurücksetzen
                IsDefaultScreenshotApp = !IsDefaultScreenshotApp;
                UpdateDefaultAppStatus();
                
                System.Diagnostics.Debug.WriteLine($"Ausnahme bei Standard-App-Änderung: {ex.Message}");
                MessageBox.Show($"Ein unerwarteter Fehler ist aufgetreten:\n{ex.Message}", 
                              "Unerwarteter Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessingDefaultAppChange = false;
            }
        }

        /// <summary>
        /// Erstellt eine detaillierte Statusmeldung
        /// </summary>
        private string CreateStatusMessage(bool enabled, bool success)
        {
            if (enabled && success)
            {
                var message = "? ScreenshotProMax wurde erfolgreich als Standard-Screenshot-App gesetzt!\n\n" +
                            "Folgende Änderungen wurden vorgenommen:\n" +
                            "• Windows Snipping Tool deaktiviert\n" +
                            "• Print Screen-Taste wird abgefangen\n" +
                            "• Win + Shift + S deaktiviert\n" +
                            "• Zusätzliche Windows Screenshot-Features deaktiviert\n" +
                            "• Aktive Snipping Tool Prozesse beendet\n" +
                            "• Kontinuierliche Prozess-Überwachung gestartet\n\n";

                if (!IsRunningAsAdministrator)
                {
                    message += "? Hinweis: Einige Änderungen erfordern Administrator-Rechte für optimale Funktionalität.\n\n";
                }
                else
                {
                    message += "? Alle Änderungen wurden mit Administrator-Rechten angewendet.\n\n";
                }

                message += "Die Prozess-Überwachung verhindert automatisch, dass das Snipping Tool gestartet wird.";

                return message;
            }
            else if (!enabled && success)
            {
                return "? ScreenshotProMax wurde als Standard-Screenshot-App deaktiviert.\n\n" +
                       "Folgende Änderungen wurden vorgenommen:\n" +
                       "• Windows Snipping Tool reaktiviert\n" +
                       "• Print Screen-Taste funktioniert wieder normal\n" +
                       "• Win + Shift + S reaktiviert\n" +
                       "• Windows Screenshot-Features reaktiviert\n" +
                       "• Prozess-Überwachung beendet\n\n" +
                       "Windows verwendet jetzt wieder die Standard-Screenshot-Tools.";
            }

            return "Unbekannter Status.";
        }

        /// <summary>
        /// Erstellt eine Fehlermeldung mit Lösungsvorschlägen
        /// </summary>
        private string CreateErrorMessage(bool attemptedToEnable)
        {
            var action = attemptedToEnable ? "aktivieren" : "deaktivieren";
            var message = $"Fehler beim {action} der Standard-Screenshot-App Einstellung.\n\n";

            if (!IsRunningAsAdministrator)
            {
                message += "Mögliche Lösungen:\n" +
                          "• Starten Sie ScreenshotProMax als Administrator\n" +
                          "• Überprüfen Sie die Windows-Benutzerkontensteuerung (UAC)\n" +
                          "• Stellen Sie sicher, dass Sie über ausreichende Berechtigungen verfügen\n\n" +
                          "Möchten Sie die Anwendung als Administrator neu starten?";
                          
                var result = MessageBox.Show(message, "Administrator-Rechte erforderlich", 
                                           MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    ((SettingsService)_settingsService).RequestAdministratorRights();
                }
                
                return ""; // Keine weitere Meldung anzeigen
            }
            else
            {
                message += "Mögliche Ursachen:\n" +
                          "• Registry-Zugriff wurde verweigert\n" +
                          "• Windows-Gruppenrichtlinien blockieren die Änderung\n" +
                          "• Antivirus-Software hat den Zugriff blockiert\n" +
                          "• System-Dateien sind beschädigt\n\n" +
                          "Versuchen Sie, Windows neu zu starten und es erneut zu versuchen.";
            }

            return message;
        }

        [RelayCommand]
        private void LangSplBtnSelectionChanged()
        {
            if (SelectedCulture != null)
            {
                _settings_service_SetAppLanguage(SelectedCulture);
                Thread.CurrentThread.CurrentCulture = SelectedCulture;
                Thread.CurrentThread.CurrentUICulture = SelectedCulture;

                // Update resource manager culture
                LanguageGUI.Culture = SelectedCulture;

                // Update application-wide localized strings
                App.Loc.Refresh();
            }
        }

        [RelayCommand]
        private void PreviewMouseDown()
        {
            // Wird beim Öffnen des DropDowns aufgerufen
        }

        [RelayCommand]
        private void RequestAdministratorRights()
        {
            try
            {
                var result = MessageBox.Show(
                    "ScreenshotProMax wird mit Administrator-Rechten neu gestartet, um vollständigen Zugriff auf die Windows-Screenshot-Einstellungen zu erhalten.\n\n" +
                    "Möchten Sie fortfahren?",
                    "Administrator-Rechte anfordern",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    ((SettingsService)_settingsService).RequestAdministratorRights();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Anfordern der Administrator-Rechte:\n{ex.Message}", 
                              "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ShowDefaultAppHelp()
        {
            var helpMessage = "Standard-Screenshot-App Einstellung:\n\n" +
                            "? Aktiviert:\n" +
                            "• ScreenshotProMax fängt die Print Screen-Taste ab\n" +
                            "• Windows Snipping Tool wird deaktiviert\n" +
                            "• Win + Shift + S wird deaktiviert\n" +
                            "• Vollständige Kontrolle über Screenshots\n" +
                            "• Aktive Prozess-Überwachung verhindert Snipping Tool Starts\n" +
                            "• Kontinuierliche Unterdrückung störender Screenshot-Tools\n\n" +
                            "? Deaktiviert:\n" +
                            "• Windows Standard-Screenshot-Tools sind aktiv\n" +
                            "• Print Screen öffnet das Snipping Tool\n" +
                            "• Win + Shift + S funktioniert normal\n" +
                            "• ScreenshotProMax funktioniert parallel\n\n" +
                            "Hinweis: Die Prozess-Überwachung erfolgt ohne Administrator-Rechte und verhindert effektiv störende Snipping Tool Aktivierungen.";

            MessageBox.Show(helpMessage, "Hilfe - Standard-Screenshot-App", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private async void CheckSnippingToolStatus()
        {
            try
            {
                ProcessMonitoringStatus = "Prüfung...";
                
                var isRunning = await Task.Run(() => SnippingToolProcessManager.IsSnippingToolRunning());
                
                if (IsDefaultScreenshotApp)
                {
                    if (isRunning)
                    {
                        var terminated = await Task.Run(() => SnippingToolProcessManager.TerminateSnippingToolProcesses());
                        ProcessMonitoringStatus = $"? {terminated} Prozesse beendet";
                        
                        MessageBox.Show($"Snipping Tool Status geprüft:\n{terminated} aktive Prozesse wurden beendet.", 
                                      "Prozess-Status", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        ProcessMonitoringStatus = "? Überwacht";
                        MessageBox.Show("Snipping Tool Status: Keine aktiven Prozesse gefunden.\nÜberwachung läuft normal.", 
                                      "Prozess-Status", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    ProcessMonitoringStatus = "Inaktiv";
                    MessageBox.Show("Standard-App-Funktion ist deaktiviert.\nKeine Snipping Tool Überwachung aktiv.", 
                                  "Prozess-Status", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ProcessMonitoringStatus = "Fehler";
                MessageBox.Show($"Fehler bei der Snipping Tool Status-Prüfung:\n{ex.Message}", 
                              "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // helper methods for bindings that cannot directly reference service (keeps XAML friendly)
        private string _settings_service_AppVersion() => _settingsService.AppVersion();

        // wrapper to call service without breaking generated code expectations
        private bool _settings_service_SetAppLanguage(CultureInfo ci) => _settingsService.SetAppLanguage(ci);
    }
}
