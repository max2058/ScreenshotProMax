using ScreenshotProMax.Interfaces;
using ScreenshotProMax.Properties;
using System.Globalization;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ScreenshotProMax.Services
{
	/// <summary>
	/// Stellt einen Dienst bereit, um Programmeinstellungen zu verwalten und zu speichern
	/// </summary>
	public class SettingsService : ISettingsService
	{
		/// <summary>
		/// Erstellt eine neue Instanz des Einstellungsdienstes
		/// </summary>
		public SettingsService()
		{
		}

		/// <summary>
		/// Ermittelt die aktuelle Version der Anwendung
		/// </summary>
		/// <returns>Die Versionsnummer im Format "vX.X.X"</returns>
		public string AppVersion()
		{
			var version = GetType().Assembly.GetName().Version;
			return version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";
		}

		/// <summary>
		/// Ruft die aktuell eingestellte Sprache der Anwendung ab
		/// </summary>
		/// <returns>Die Kulturinformationen der eingestellten Sprache</returns>
		public CultureInfo GetAppLanguage()
		{
			var cultureName = Settings.Default.CultureLang;
			if (string.IsNullOrEmpty(cultureName))
			{
				return new CultureInfo("de-DE");
			}
			return new CultureInfo(cultureName);
		}

		/// <summary>
		/// Setzt die Sprache der Anwendung auf die angegebene Kultur
		/// </summary>
		/// <param name="AppLang">Die zu verwendende Kultur</param>
		/// <returns>True wenn die Änderung erfolgreich gespeichert wurde</returns>
		public bool SetAppLanguage(CultureInfo AppLang)
		{
			Settings.Default.CultureLang = AppLang.Name;
			Settings.Default.Save();
			return true;
		}

		/// <summary>
		/// Setzt das Basis-Design-Theme der Anwendung
		/// </summary>
		/// <param name="BaseTheme">Das zu verwendende Basis-Theme</param>
		/// <returns>True wenn die Änderung erfolgreich gespeichert wurde</returns>
		public bool SetAppBaseTheme(string BaseTheme)
		{
			Settings.Default.MainDesignStyle = BaseTheme;
			Settings.Default.Save();
			return true;
		}

		public string GetAppBaseTheme()
		{
			return Settings.Default.MainDesignStyle ?? "Dark";
		}

		/// <summary>
		/// Ermittelt ob diese Anwendung als Standard-Screenshot-App gesetzt ist
		/// </summary>
		/// <returns>True wenn die Anwendung als Standard gesetzt ist</returns>
		public bool GetIsDefaultScreenshotApp()
		{
			return Settings.Default.IsDefaultScreenshotApp;
		}

		/// <summary>
		/// Setzt diese Anwendung als Standard-Screenshot-App oder deaktiviert sie
		/// </summary>
		/// <param name="isDefault">True um die Anwendung als Standard zu setzen, False um Windows Standard zu verwenden</param>
		/// <returns>True wenn die Änderung erfolgreich gespeichert wurde</returns>
		public bool SetIsDefaultScreenshotApp(bool isDefault)
		{
			try
			{
				if (isDefault)
				{
					// Umfassende Deaktivierung aller Windows Screenshot-Tools
					return EnableAsDefaultScreenshotApp();
				}
				else
				{
					// Reaktivierung der Windows Screenshot-Tools
					return DisableAsDefaultScreenshotApp();
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Fehler beim Setzen der Standard-Screenshot-App: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// Aktiviert ScreenshotProMax als Standard-Screenshot-App
		/// </summary>
		private bool EnableAsDefaultScreenshotApp()
		{
			bool overallSuccess = true;
			var results = new System.Collections.Generic.List<string>();

			try
			{
				// 1. Snipping Tool deaktivieren
				if (SnippingToolControl.SetSnippingToolEnabled(false))
				{
					results.Add("✓ Snipping Tool deaktiviert");
				}
				else
				{
					results.Add("⚠ Snipping Tool konnte nicht vollständig deaktiviert werden");
					overallSuccess = false;
				}

				// 2. Print Screen für Snipping Tool deaktivieren
				if (SnippingToolControl.SetPrintScreenForSnippingTool(false))
				{
					results.Add("✓ Print Screen für Snipping Tool deaktiviert");
				}
				else
				{
					results.Add("⚠ Print Screen für Snipping Tool konnte nicht deaktiviert werden");
					overallSuccess = false;
				}

				// 3. Win + Shift + S deaktivieren
				if (SnippingToolControl.SetWinShiftSHotkeyEnabled(false))
				{
					results.Add("✓ Win + Shift + S Hotkey deaktiviert");
				}
				else
				{
					results.Add("⚠ Win + Shift + S Hotkey konnte nicht deaktiviert werden");
					overallSuccess = false;
				}

				// 4. Zusätzliche Windows Screenshot-Features deaktivieren
				if (SnippingToolControl.DisableWindowsScreenshotFeatures())
				{
					results.Add("✓ Zusätzliche Windows Screenshot-Features deaktiviert");
				}
				else
				{
					results.Add("⚠ Einige zusätzliche Screenshot-Features konnten nicht deaktiviert werden");
					overallSuccess = false;
				}

				// 5. Snipping Tool Prozesse beenden (NEUE FUNKTION)
				var terminatedProcesses = SnippingToolProcessManager.TerminateSnippingToolProcesses();
				if (terminatedProcesses > 0)
				{
					results.Add($"✓ {terminatedProcesses} Snipping Tool Prozesse beendet");
				}
				else
				{
					results.Add("✓ Keine aktiven Snipping Tool Prozesse gefunden");
				}

				// 6. Erweiterte Prozess-Überwachung starten (NEUE FUNKTION)
				SnippingToolProcessManager.StartAdvancedMonitoring();
				results.Add("✓ Kontinuierliche Snipping Tool Überwachung gestartet");

				// 7. Als Standard-App registrieren (optional)
				RegisterAsDefaultScreenshotApp();
				results.Add("✓ Als Standard-Screenshot-App registriert");

				// Einstellung speichern nur wenn alles erfolgreich war
				if (overallSuccess)
				{
					Settings.Default.IsDefaultScreenshotApp = true;
					Settings.Default.Save();
					System.Diagnostics.Debug.WriteLine("ScreenshotProMax erfolgreich als Standard-Screenshot-App aktiviert");
				}

				// Detaillierte Ergebnisse ausgeben
				foreach (var result in results)
				{
					System.Diagnostics.Debug.WriteLine(result);
				}

				return overallSuccess;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Fehler beim Aktivieren als Standard-App: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// Deaktiviert ScreenshotProMax als Standard-Screenshot-App und reaktiviert Windows-Tools
		/// </summary>
		private bool DisableAsDefaultScreenshotApp()
		{
			bool overallSuccess = true;
			var results = new System.Collections.Generic.List<string>();

			try
			{
				// 1. Snipping Tool reaktivieren
				if (SnippingToolControl.SetSnippingToolEnabled(true))
				{
					results.Add("✓ Snipping Tool reaktiviert");
				}
				else
				{
					results.Add("⚠ Snipping Tool konnte nicht reaktiviert werden");
					overallSuccess = false;
				}

				// 2. Print Screen für Snipping Tool reaktivieren
				if (SnippingToolControl.SetPrintScreenForSnippingTool(true))
				{
					results.Add("✓ Print Screen für Snipping Tool reaktiviert");
				}
				else
				{
					results.Add("⚠ Print Screen für Snipping Tool konnte nicht reaktiviert werden");
					overallSuccess = false;
				}

				// 3. Win + Shift + S reaktivieren
				if (SnippingToolControl.SetWinShiftSHotkeyEnabled(true))
				{
					results.Add("✓ Win + Shift + S Hotkey reaktiviert");
				}
				else
				{
					results.Add("⚠ Win + Shift + S Hotkey konnte nicht reaktiviert werden");
					overallSuccess = false;
				}

				// 4. Windows Screenshot-Features reaktivieren
				if (SnippingToolControl.EnableWindowsScreenshotFeatures())
				{
					results.Add("✓ Windows Screenshot-Features reaktiviert");
				}
				else
				{
					results.Add("⚠ Einige Windows Screenshot-Features konnten nicht reaktiviert werden");
					overallSuccess = false;
				}

				// 5. Standard-App-Registrierung entfernen
				UnregisterAsDefaultScreenshotApp();
				results.Add("✓ Standard-Screenshot-App-Registrierung entfernt");

				// Hinweis: Prozess-Überwachung wird automatisch gestoppt wenn die App als Standard deaktiviert wird

				// Einstellung immer speichern
				Settings.Default.IsDefaultScreenshotApp = false;
				Settings.Default.Save();

				// Detaillierte Ergebnisse ausgeben
				foreach (var result in results)
				{
					System.Diagnostics.Debug.WriteLine(result);
				}

				System.Diagnostics.Debug.WriteLine("ScreenshotProMax als Standard-Screenshot-App deaktiviert");
				return overallSuccess;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Fehler beim Deaktivieren als Standard-App: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// Überprüft beim Anwendungsstart, ob Snipping Tool Prozesse laufen und beendet sie falls nötig
		/// </summary>
		public void CheckAndTerminateSnippingToolOnStartup()
		{
			try
			{
				bool isDefaultApp = GetIsDefaultScreenshotApp();
				
				if (isDefaultApp)
				{
					System.Diagnostics.Debug.WriteLine("ScreenshotProMax ist als Standard-App gesetzt - prüfe Snipping Tool Prozesse");
					
					if (SnippingToolProcessManager.IsSnippingToolRunning())
					{
						var terminated = SnippingToolProcessManager.TerminateSnippingToolProcesses();
						System.Diagnostics.Debug.WriteLine($"Beim Anwendungsstart {terminated} Snipping Tool Prozesse beendet");
					}
					
					// Starte kontinuierliche Überwachung
					SnippingToolProcessManager.StartAdvancedMonitoring();
					System.Diagnostics.Debug.WriteLine("Kontinuierliche Snipping Tool Überwachung beim Start aktiviert");
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("ScreenshotProMax ist nicht als Standard-App gesetzt - keine Snipping Tool Prozess-Überwachung");
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Fehler bei Startup-Snipping-Tool-Check: {ex.Message}");
			}
		}

		/// <summary>
		/// Führt eine sofortige aggressive Unterdrückung von Snipping Tool Prozessen durch (bei Print Screen)
		/// </summary>
		public async Task AggressiveSnippingToolSuppressionAsync()
		{
			try
			{
				bool isDefaultApp = GetIsDefaultScreenshotApp();
				
				if (isDefaultApp)
				{
					System.Diagnostics.Debug.WriteLine("Print Screen erkannt - starte aggressive Snipping Tool Unterdrückung");
					await SnippingToolProcessManager.AggressiveSnippingToolSuppression();
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Fehler bei aggressiver Snipping Tool Unterdrückung: {ex.Message}");
			}
		}

		/// <summary>
		/// Registriert diese Anwendung als Standard für Screenshots (erweiterte Version)
		/// </summary>
		private void RegisterAsDefaultScreenshotApp()
		{
			try
			{
				var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
				if (string.IsNullOrEmpty(exePath))
					return;

				// 1. Registriere als Standard-App für .png Dateien
				using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.png\UserChoice"))
				{
					key?.SetValue("ProgId", "ScreenshotProMax.Image");
				}

				// 2. Erstelle ProgID für Bilddateien
				using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\ScreenshotProMax.Image"))
				{
					key?.SetValue("", "ScreenshotProMax Screenshot");
					
					using (var shellKey = key?.CreateSubKey(@"shell\open\command"))
					{
						shellKey?.SetValue("", $"\"{exePath}\" \"%1\"");
					}

					using (var iconKey = key?.CreateSubKey(@"DefaultIcon"))
					{
						iconKey?.SetValue("", $"\"{exePath}\",0");
					}
				}

				// 3. Als Standardanwendung für Screenshot-Dateitypen registrieren
				var extensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
				foreach (var ext in extensions)
				{
					try
					{
						using (var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{ext}\OpenWithProgids"))
						{
							extKey?.SetValue("ScreenshotProMax.Image", new byte[0], RegistryValueKind.None);
						}
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"Fehler bei der Registrierung für {ext}: {ex.Message}");
					}
				}

				System.Diagnostics.Debug.WriteLine("Erweiterte Standard-App-Registrierung abgeschlossen");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Fehler bei der erweiterten Standard-App-Registrierung: {ex.Message}");
			}
		}

		/// <summary>
		/// Entfernt die Registrierung als Standard-Screenshot-App (erweiterte Version)
		/// </summary>
		private void UnregisterAsDefaultScreenshotApp()
		{
			try
			{
				// Entferne PNG-Datei-Zuordnung
				try
				{
					Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.png\UserChoice", false);
				}
				catch (ArgumentException) { /* Key existiert nicht */ }

				// Entferne ProgID
				try
				{
					Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\ScreenshotProMax.Image", false);
				}
				catch (ArgumentException) { /* Key existiert nicht */ }

				// Entferne Zuordnungen für verschiedene Dateiformate
				var extensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
				foreach (var ext in extensions)
				{
					try
					{
						using (var extKey = Registry.CurrentUser.OpenSubKey($@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{ext}\OpenWithProgids", true))
						{
							extKey?.DeleteValue("ScreenshotProMax.Image", false);
						}
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"Fehler beim Entfernen der {ext}-Zuordnung: {ex.Message}");
					}
				}

				System.Diagnostics.Debug.WriteLine("Standard-Screenshot-App-Registrierung erfolgreich entfernt");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Fehler beim Entfernen der Standard-App-Registrierung: {ex.Message}");
			}
		}

		/// <summary>
		/// Überprüft ob Administrator-Rechte verfügbar sind
		/// </summary>
		/// <returns>True wenn Administrator-Rechte verfügbar sind</returns>
		public bool IsAdministratorRightsAvailable()
		{
			return SnippingToolControl.IsRunningAsAdministrator();
		}

		/// <summary>
		/// Fordert Administrator-Rechte an und startet die Anwendung neu
		/// </summary>
		/// <returns>True wenn der Neustart erfolgreich eingeleitet wurde</returns>
		public bool RequestAdministratorRights()
		{
			try
			{
				SnippingToolControl.RestartAsAdministrator();
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Ermittelt ob das initiale Setup bereits abgeschlossen wurde
		/// </summary>
		/// <returns>True wenn das Setup bereits durchgeführt wurde</returns>
		public bool GetIsInitialSetupCompleted()
		{
			return Settings.Default.IsInitialSetupCompleted;
		}

		/// <summary>
		/// Markiert das initiale Setup als abgeschlossen
		/// </summary>
		/// <returns>True wenn die Änderung erfolgreich gespeichert wurde</returns>
		public bool SetInitialSetupCompleted(bool completed)
		{
			Settings.Default.IsInitialSetupCompleted = completed;
			Settings.Default.Save();
			return true;
		}

		/// <summary>
		/// Setzt alle Benutzereinstellungen auf die Standardwerte zurück
		/// </summary>
		/// <returns>True wenn das Zurücksetzen erfolgreich war</returns>
		public bool ResetAllUserSettings()
		{
			try
			{
				Settings.Default.Reset();
				Settings.Default.Save();
				System.Diagnostics.Debug.WriteLine("Alle Benutzereinstellungen wurden zurückgesetzt");
				return true;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Fehler beim Zurücksetzen der Benutzereinstellungen: {ex.Message}");
				return false;
			}
		}
	}
}
