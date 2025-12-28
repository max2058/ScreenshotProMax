using ScreenshotProMax.Services;
using System;

namespace ScreenshotProMax.Testing
{
    /// <summary>
    /// Test helper class to reset the initial setup flag for testing purposes
    /// </summary>
    public static class SetupTestHelper
    {
        /// <summary>
        /// Resets the initial setup flag so the setup window will be shown on next startup
        /// Call this method for testing the initial setup flow
        /// </summary>
        public static void ResetInitialSetup()
        {
            var settingsService = new SettingsService();
            settingsService.SetInitialSetupCompleted(false);
            Console.WriteLine("Initial setup flag has been reset. The setup window will appear on next startup.");
        }

        /// <summary>
        /// Resets all user settings to their default values
        /// This is useful for testing with completely clean settings
        /// </summary>
        public static void ResetAllSettings()
        {
            var settingsService = new SettingsService();
            settingsService.ResetAllUserSettings();
            Console.WriteLine("All user settings have been reset to defaults.");
        }
    }
}