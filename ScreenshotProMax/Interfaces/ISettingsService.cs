using System.Globalization;
using System.Threading.Tasks;

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

		/// <summary>
		/// Ermittelt ob diese Anwendung als Standard-Screenshot-App gesetzt ist
		/// </summary>
		/// <returns>True wenn die Anwendung als Standard gesetzt ist</returns>
		bool GetIsDefaultScreenshotApp();

		/// <summary>
		/// Setzt diese Anwendung als Standard-Screenshot-App oder deaktiviert sie
		/// Implementiert umfassende Registry-Manipulation zur Deaktivierung aller Windows Screenshot-Tools
		/// und aktive Prozess-Überwachung zur Verhinderung von Snipping Tool Starts
		/// </summary>
		/// <param name="isDefault">True um die Anwendung als Standard zu setzen, False um Windows Standard zu verwenden</param>
		/// <returns>True wenn die Änderung erfolgreich gespeichert wurde</returns>
		bool SetIsDefaultScreenshotApp(bool isDefault);

		/// <summary>
		/// Ermittelt ob das initiale Setup bereits abgeschlossen wurde
		/// </summary>
		/// <returns>True wenn das Setup bereits durchgeführt wurde</returns>
		bool GetIsInitialSetupCompleted();

		/// <summary>
		/// Markiert das initiale Setup als abgeschlossen
		/// </summary>
		/// <param name="completed">True wenn das Setup abgeschlossen wurde</param>
		/// <returns>True wenn die Änderung erfolgreich gespeichert wurde</returns>
		bool SetInitialSetupCompleted(bool completed);
	}
}
