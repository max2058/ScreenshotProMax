using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Security.Principal;

namespace ScreenshotProMax.Services
{
    /// <summary>
    /// Stellt Funktionen zur Kontrolle des Windows Snipping Tools bereit
    /// </summary>
    public static class SnippingToolControl
    {
        /// <summary>
        /// Aktiviert oder deaktiviert das Windows Snipping Tool über die Registry
        /// </summary>
        /// <param name="enabled">True um das Snipping Tool zu aktivieren, False zum Deaktivieren</param>
        /// <returns>True wenn erfolgreich, False bei Fehlern</returns>
        public static bool SetSnippingToolEnabled(bool enabled)
        {
            try
            {
                // Versuche zuerst die HKEY_CURRENT_USER Variante (keine Admin-Rechte erforderlich)
                if (SetSnippingToolEnabledCurrentUser(enabled))
                {
                    System.Diagnostics.Debug.WriteLine($"Snipping Tool {(enabled ? "aktiviert" : "deaktiviert")} (Current User)");
                    return true;
                }

                // Wenn das nicht funktioniert, versuche HKEY_LOCAL_MACHINE (Admin-Rechte erforderlich)
                if (IsRunningAsAdministrator())
                {
                    return SetSnippingToolEnabledLocalMachine(enabled);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Administrator-Rechte erforderlich für vollständige Snipping Tool-Kontrolle");
                    // Versuche trotzdem die Benutzer-spezifischen Einstellungen
                    return SetSnippingToolEnabledCurrentUser(enabled);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Setzen des Snipping Tool Status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Aktiviert/deaktiviert das Snipping Tool für den aktuellen Benutzer
        /// </summary>
        private static bool SetSnippingToolEnabledCurrentUser(bool enabled)
        {
            try
            {
                string keyPath = @"Software\Policies\Microsoft\TabletPC";
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath))
                {
                    if (key != null)
                    {
                        key.SetValue("DisableSnippingTool", enabled ? 0 : 1, RegistryValueKind.DWord);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei CurrentUser Registry-Zugriff: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Aktiviert/deaktiviert das Snipping Tool systemweit (benötigt Admin-Rechte)
        /// </summary>
        private static bool SetSnippingToolEnabledLocalMachine(bool enabled)
        {
            try
            {
                string keyPath = @"SOFTWARE\Policies\Microsoft\TabletPC";
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (RegistryKey key = baseKey.CreateSubKey(keyPath))
                {
                    if (key != null)
                    {
                        key.SetValue("DisableSnippingTool", enabled ? 0 : 1, RegistryValueKind.DWord);
                        System.Diagnostics.Debug.WriteLine($"Snipping Tool {(enabled ? "aktiviert" : "deaktiviert")} (Local Machine)");
                        return true;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine("Zugriff verweigert. Administrator-Rechte erforderlich.");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei LocalMachine Registry-Zugriff: {ex.Message}");
                return false;
            }
            
            return false;
        }

        /// <summary>
        /// Aktiviert/deaktiviert die Print Screen-Taste für das Snipping Tool
        /// </summary>
        public static bool SetPrintScreenForSnippingTool(bool enabled)
        {
            try
            {
                string keyPath = @"Control Panel\Keyboard";
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath))
                {
                    if (key != null)
                    {
                        if (enabled)
                        {
                            // Entferne den Wert oder setze ihn auf 1
                            key.SetValue("PrintScreenKeyForSnippingEnabled", 1, RegistryValueKind.DWord);
                        }
                        else
                        {
                            // Deaktiviere die Print Screen-Taste für Snipping Tool
                            key.SetValue("PrintScreenKeyForSnippingEnabled", 0, RegistryValueKind.DWord);
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"Print Screen für Snipping Tool {(enabled ? "aktiviert" : "deaktiviert")}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Setzen der Print Screen-Einstellung: {ex.Message}");
                return false;
            }
            return false;
        }

        /// <summary>
        /// Aktiviert/deaktiviert die Win + Shift + S Tastenkombination
        /// </summary>
        public static bool SetWinShiftSHotkeyEnabled(bool enabled)
        {
            try
            {
                string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath))
                {
                    if (key != null)
                    {
                        if (!enabled)
                        {
                            // Deaktiviere Win + Shift + S
                            key.SetValue("DisabledHotkeys", "S", RegistryValueKind.String);
                            System.Diagnostics.Debug.WriteLine("Win + Shift + S Hotkey deaktiviert");
                        }
                        else
                        {
                            // Aktiviere Win + Shift + S wieder
                            try
                            {
                                key.DeleteValue("DisabledHotkeys", false);
                                System.Diagnostics.Debug.WriteLine("Win + Shift + S Hotkey aktiviert");
                            }
                            catch (ArgumentException)
                            {
                                // Wert existiert nicht, das ist OK
                                System.Diagnostics.Debug.WriteLine("Win + Shift + S Hotkey war bereits aktiviert");
                            }
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Setzen der Win+Shift+S Einstellung: {ex.Message}");
                return false;
            }
            return false;
        }

        /// <summary>
        /// Deaktiviert zusätzliche Windows Screenshot-Funktionen
        /// </summary>
        public static bool DisableWindowsScreenshotFeatures()
        {
            bool success = true;
            
            try
            {
                // Deaktiviere GameDVR Screenshots
                string gameDVRPath = @"Software\Microsoft\Windows\CurrentVersion\GameDVR";
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(gameDVRPath))
                {
                    key?.SetValue("AppCaptureEnabled", 0, RegistryValueKind.DWord);
                    key?.SetValue("HistoricalCaptureEnabled", 0, RegistryValueKind.DWord);
                }

                // Deaktiviere Xbox Game Bar Screenshot-Hotkeys
                string gameBarPath = @"Software\Microsoft\GameBar";
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(gameBarPath))
                {
                    key?.SetValue("UseNexusForGameBarEnabled", 0, RegistryValueKind.DWord);
                }

                System.Diagnostics.Debug.WriteLine("Zusätzliche Windows Screenshot-Features deaktiviert");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Deaktivieren zusätzlicher Screenshot-Features: {ex.Message}");
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Reaktiviert alle Windows Screenshot-Funktionen
        /// </summary>
        public static bool EnableWindowsScreenshotFeatures()
        {
            bool success = true;
            
            try
            {
                // Reaktiviere GameDVR Screenshots
                string gameDVRPath = @"Software\Microsoft\Windows\CurrentVersion\GameDVR";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(gameDVRPath, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("AppCaptureEnabled", false);
                        key.DeleteValue("HistoricalCaptureEnabled", false);
                    }
                }

                // Reaktiviere Xbox Game Bar
                string gameBarPath = @"Software\Microsoft\GameBar";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(gameBarPath, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("UseNexusForGameBarEnabled", false);
                    }
                }

                System.Diagnostics.Debug.WriteLine("Windows Screenshot-Features reaktiviert");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Reaktivieren der Screenshot-Features: {ex.Message}");
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Überprüft, ob die Anwendung mit Administrator-Rechten läuft
        /// </summary>
        public static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Startet die Anwendung mit Administrator-Rechten neu
        /// </summary>
        public static void RestartAsAdministrator()
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = Process.GetCurrentProcess().MainModule?.FileName ?? "",
                    UseShellExecute = true,
                    Verb = "runas" // Fordert Administrator-Rechte an
                };

                Process.Start(processInfo);
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Neustart als Administrator: {ex.Message}");
            }
        }
    }
}