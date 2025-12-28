using System;
using System.Windows;
using MahApps.Metro.Controls;
using ScreenshotProMax.Services;
using ControlzEx.Theming;

namespace ScreenshotProMax.Views
{
    /// <summary>
    /// Initial setup window that is shown on the first application startup
    /// </summary>
    public partial class InitialSetupWindow : MetroWindow
    {
        private readonly SettingsService _settingsService;

        public InitialSetupWindow()
        {
            InitializeComponent();
            _settingsService = new SettingsService();
            
            // Apply current theme to this window
            ApplyCurrentTheme();
        }

        private void ApplyCurrentTheme()
        {
            try
            {
                var theme = _settingsService.GetAppBaseTheme();
                ThemeManager.Current.ChangeThemeBaseColor(this, theme);
            }
            catch (Exception ex)
            {
                // If theme application fails, continue with default
                Console.WriteLine($"Failed to apply theme to setup window: {ex.Message}");
            }
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Mark the initial setup as completed
                _settingsService.SetInitialSetupCompleted(true);

                // Close the setup window with a positive result
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Speichern der Einstellungen: {ex.Message}", 
                    "Fehler", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // User chose to skip initial setup for now
            // Don't mark setup as completed so it will show again next time
            DialogResult = false;
            Close();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            
            // Focus the first interactive element for better UX
            SettingsControl.Focus();
        }
    }
}