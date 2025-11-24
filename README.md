# ScreenshotProMax

Eine .NET 8 WPF Anwendung im MVVM-Stil, inspiriert von Greenshot. Die App erlaubt Bildschirmaufnahmen, schnelle Anmerkungen (Pfeile, Linien, Nummern, Text, Freihand) und das Speichern der Ergebnisse als PNG oder JPEG.

## Features
- Bildschirmfoto des primären Monitors erstellen.
- Werkzeuge: Pfeil, Linie, Text, nummerierte Marker, Freihand-Stift.
- Anpassbare Farbe, Strichstärke und Transparenz.
- Speicherung mit eingebrannten Anmerkungen.

## Projektstruktur
- `ScreenshotProMax/` – WPF-Projektdateien.
  - `Views/` – Fenster und XAML-Layout.
  - `ViewModels/` – ViewModels mit Commands und Zustand.
  - `Models/` – Annotationsmodelle.
  - `Services/` – Screenshot- und Exportdienste.
  - `Converters/` – UI-spezifische Converter.

## Voraussetzungen
- .NET SDK 8.0 mit Windows Desktop-Workload (WPF).
- Windows-Host (wegen Bildschirmaufnahme-API).

## Verwendung
1. Projekt in Visual Studio 2022 oder `dotnet build` öffnen.
2. Anwendung starten (`F5`).
3. Über „Bild aufnehmen“ ein Screenshot erstellen.
4. Mit der Toolbar Werkzeug, Farbe, Strichstärke und Transparenz wählen und auf dem Screenshot zeichnen.
5. Über „Speichern“ das Bild mit allen Anmerkungen ablegen.
