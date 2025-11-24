# ScreenshotProMax

Eine professionelle .NET 8 WPF Screenshot-Anwendung mit erweiterten Annotations-Features, inspiriert von Greenshot.

## 🎯 Features

### Screenshot-Aufnahme
- **Globaler Hotkey `Strg+D`**: Screenshot jederzeit aus jeder Anwendung starten
- **Region-Auswahl**: Freies Rechteck auf dem Bildschirm auswählen
- **Fenster-Erkennung**: Aktives Fenster automatisch erfassen
- **Einzelner Bildschirm**: Primären Monitor aufnehmen
- **Alle Bildschirme**: Multi-Monitor-Setup komplett erfassen

### Annotations-Werkzeuge
- **Pfeil**: Pfeile mit anpassbarer Dicke und Farbe
- **Linie**: Gerade Linien zeichnen
- **Text**: Texte auf Screenshots platzieren
- **Nummerierung**: Fortlaufende Nummern für Schritt-für-Schritt-Anleitungen
- **Freihand**: Freihand-Zeichnungen mit Stift-Werkzeug

### Anpassbare Eigenschaften
- **5 Farben**: Rot, Gelb, Grün, Cyan, Lila
- **Variable Dicke**: 1-12 Pixel einstellbar
- **Transparenz**: 0.2-1.0 (20%-100%)

### Export
- **PNG**: Verlustfreies Format
- **JPEG**: Komprimiertes Format
- Alle Annotations werden eingebrannt

## 🚀 Schnellstart

### Voraussetzungen
- Windows 10/11
- .NET 8.0 SDK mit Windows Desktop-Workload

### Installation
```bash
git clone https://github.com/max2058/ScreenshotProMax
cd ScreenshotProMax
dotnet build
dotnet run --project ScreenshotProMax
```

## 📖 Verwendung

### Screenshot erstellen
1. **Hotkey-Methode** (empfohlen):
   - Drücke `Strg+D` von überall
   - Ziehe ein Rechteck über den gewünschten Bereich
   - Hauptfenster öffnet sich automatisch

2. **Button-Methode**:
   - Klicke auf einen der Capture-Buttons:
     - **Region**: Bereich auswählen
     - **Fenster**: Aktives Fenster
     - **Bildschirm**: Primärer Monitor
     - **Alle Bildschirme**: Alle Monitore

### Annotationen hinzufügen
1. Wähle ein Werkzeug (Pfeil, Linie, Text, Nummer, Freihand)
2. Wähle Farbe, Dicke und Transparenz
3. Klicke und ziehe auf dem Screenshot:
   - **Pfeil/Linie**: Von Start zu Ende ziehen
   - **Nummer/Text**: Einmal klicken
   - **Freihand**: Ziehen zum Zeichnen

### Speichern
1. Klicke auf **Speichern**
2. Wähle Format (PNG/JPEG) und Speicherort
3. Alle Annotations werden automatisch eingebrannt

## 🏗️ Projektstruktur

```
ScreenshotProMax/
├── Views/
│   ├── MainWindow.xaml/xaml.cs           # Hauptfenster
│   └── RegionSelectorWindow.xaml/xaml.cs # Region-Auswahl-Overlay
├── ViewModels/
│   └── MainViewModel.cs                  # Haupt-ViewModel (MVVM)
├── Models/
│   └── AnnotationModel.cs                # Annotation-Datenmodell
├── Services/
│   ├── ScreenshotService.cs              # Screenshot-Logik
│   ├── ImageExportService.cs             # Export mit Annotations
│   ├── HotkeyService.cs                  # Globaler Hotkey-Handler
│   └── NativeMethods.cs                  # P/Invoke Windows-API
├── Converters/
│   └── PointsToPointCollectionConverter.cs # XAML-Converter
└── Themes/
    └── Colors.xaml                       # Farbschema
```

## 🔧 Technologie-Stack

- **.NET 8.0** mit WPF
- **MVVM Pattern** mit CommunityToolkit.Mvvm
- **Global Hotkeys** via Windows API (user32.dll)
- **Multi-Monitor-Support** via System.Windows.Forms.Screen
- **GDI+ Screen Capture** mit System.Drawing

## 📝 Bekannte Einschränkungen
- Hotkey `Strg+D` funktioniert nur während die Anwendung läuft
- Windows-only (WPF + GDI+ Dependencies)
- DPI-Awareness könnte auf High-DPI-Displays Anpassungen benötigen

## 🛠️ Development

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run --project ScreenshotProMax
```

### Clean
```bash
dotnet clean
```

## 📄 Lizenz

Dieses Projekt steht unter der MIT-Lizenz.

## 🤝 Beiträge

Contributions sind willkommen! Bitte erstelle einen Pull Request oder Issue.

## 📧 Kontakt

- GitHub: [@max2058](https://github.com/max2058)
- Repository: [ScreenshotProMax](https://github.com/max2058/ScreenshotProMax)
