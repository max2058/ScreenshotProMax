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

            // Reagiere auf Property-Änderungen (insbesondere IsDarkTheme)
            PropertyChanged += SettingsFlyoutViewModel_PropertyChanged;
        }

        [ObservableProperty]
        private ObservableCollection<CultureInfo> supportedCultures = new();

        [ObservableProperty]
        private CultureInfo? selectedCulture;

        [ObservableProperty]
        private bool isDarkTheme;

        [ObservableProperty]
        private string appVersion = string.Empty;

        private void LoadSupportedCultures()
        {
            SupportedCultures = new ObservableCollection<CultureInfo>
            {
                new CultureInfo("de-DE"),
                new CultureInfo("en-US"),
                new CultureInfo("pl-PL")
            };
        }

        private void SettingsFlyoutViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsDarkTheme))
            {
                var theme = IsDarkTheme ? "Dark" : "Light";
                ThemeManager.Current.ChangeThemeBaseColor(Application.Current, theme);
                _settingsService.SetAppBaseTheme(theme);
            }
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

        // helper methods for bindings that cannot directly reference service (keeps XAML friendly)
        private string _settings_service_AppVersion() => _settingsService.AppVersion();

        // wrapper to call service without breaking generated code expectations
        private bool _settings_service_SetAppLanguage(CultureInfo ci) => _settingsService.SetAppLanguage(ci);
    }
}
