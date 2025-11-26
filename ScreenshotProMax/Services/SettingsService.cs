using ScreenshotProMax.Interfaces;
using ScreenshotProMax.Properties;
using System.Globalization;

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
	}
}
