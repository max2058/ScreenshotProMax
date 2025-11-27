# Initial Setup Window - Funktionalität

## Übersicht
Die Anwendung zeigt beim ersten Start ein Initial Setup Fenster an, in dem der Nutzer alle wichtigen Einstellungen konfigurieren kann, bevor er die Hauptanwendung verwendet.

## Implementierte Features

### 1. InitialSetupWindow
- **Datei**: `Views/InitialSetupWindow.xaml` & `Views/InitialSetupWindow.xaml.cs`
- Modales Fenster mit integriertem SettingsFlyout UserControl
- Willkommenstext und wichtige Hinweise für neue Nutzer
- "Fertig" Button zum Abschließen des Setups
- "Später" Button zum Überspringen (Setup wird beim nächsten Start erneut angezeigt)

### 2. Einstellungen für Initial Setup
- **Neue Einstellung**: `IsInitialSetupCompleted` (Boolean)
- **Gespeichert in**: `Properties/Settings.settings`
- **Service-Methoden**: `GetIsInitialSetupCompleted()`, `SetInitialSetupCompleted(bool)`

### 3. Startup-Logik
- **App.xaml.cs** wurde erweitert um Initial Setup Check
- Setup wird nur beim ersten Start oder wenn Flag auf `false` gesetzt ist angezeigt
- Nach erfolgreichem Setup werden Sprache und Theme neu geladen

## Verfügbare Einstellungen im Setup

Das Initial Setup Fenster zeigt alle Einstellungen aus dem `SettingsFlyout` UserControl:

1. **Sprache**: Deutsch, Englisch, Polnisch
2. **Theme**: Light/Dark Mode mit visuellen Indikatoren (Sonne/Mond Icons)
3. **Standard-Screenshot-App**: Option zur Aktivierung der Druck-Taste für Screenshots

## Testing

### Setup zurücksetzen für Tests
Um das Initial Setup erneut zu testen, können Sie das Setup-Flag zurücksetzen:

```csharp
// Verwenden Sie die Test-Helper Klasse
ScreenshotProMax.Testing.SetupTestHelper.ResetInitialSetup();
```

Oder manuell über die Einstellungen:
```csharp
var settingsService = new SettingsService();
settingsService.SetInitialSetupCompleted(false);
```

### Erwartetes Verhalten
1. **Erster Start**: Setup-Fenster wird angezeigt
2. **Setup abgeschlossen**: Flag wird auf `true` gesetzt, Setup wird nicht mehr angezeigt
3. **Setup übersprungen**: Flag bleibt `false`, Setup wird beim nächsten Start erneut angezeigt
4. **Einstellungen während Setup**: Werden sofort angewendet und gespeichert

## Technische Details

### Abhängigkeiten
- MahApps.Metro für UI-Styling
- ControlzEx für Theme-Management
- Bestehende SettingsFlyout UserControl wird wiederverwendet

### Dateistruktur
```
Views/
??? InitialSetupWindow.xaml          # UI Definition
??? InitialSetupWindow.xaml.cs       # Code-Behind
??? ...

Services/
??? SettingsService.cs               # Erweitert um Setup-Methoden
??? ...

Properties/
??? Settings.Designer.cs             # Auto-generiert, enthält neue Property
??? Settings.settings                # Einstellungs-Definitionen

Testing/
??? SetupTestHelper.cs               # Helper für Tests
```

### Integration mit bestehendem Code
- Nutzt bestehende Infrastruktur (SettingsService, SettingsFlyout)
- Keine Breaking Changes an bestehenden Funktionen
- Theme und Lokalisierung werden korrekt angewendet