using System.Globalization;

namespace ScreenshotProMax.Interfaces
{
	/// <summary>
	/// Definiert die Schnittstelle für einen Dienst zur Verwaltung von Programmeinstellungen
	/// </summary>
	public interface ISettingsService
	{
		/// <summary>
		/// Ermittelt die Version der Anwendung
		/// </summary>
		/// <returns>Die Versionsnummer im Format "vX.X.X"</returns>
		string AppVersion();

		/// <summary>
		/// Ermittelt die genutzte Applikations Sprache
		/// </summary>
		/// <returns>Die Kulturinformationen der eingestellten Sprache</returns>
		CultureInfo GetAppLanguage();

		/// <summary>
		/// Setzt die Sprache der Anwendung auf die angegebene Kultur
		/// </summary>
		/// <param name="AppLang">Die zu verwendende Kultur</param>
		/// <returns>True wenn die Änderung erfolgreich gespeichert wurde</returns>
		bool SetAppLanguage(CultureInfo AppLang);

		/// <summary>
		/// Setzt das Basis-Design-Theme der Anwendung
		/// </summary>
		/// <param name="BaseTheme">Das zu verwendende Basis-Theme</param>
		/// <returns>True wenn die Änderung erfolgreich gespeichert wurde</returns>
		bool SetAppBaseTheme(string BaseTheme);

		/// <summary>
		/// Liefert das aktuell gesetzte Basis-Theme (Light/Dark)
		/// </summary>
		string GetAppBaseTheme();
	}
}
