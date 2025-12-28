# ScreenshotProMax

A professional **.NET 8 WPF screenshot application** with advanced annotation features, inspired by Greenshot.  
The application definitely has some rough edges and limitations here and there.

## 🎯 Features

### Screenshot Capture
- **Global hotkey `Ctrl + D`**: Start a screenshot from anywhere, in any application
- **Region selection**: Select a free rectangle area on the screen
- **Window detection**: Automatically capture the active window
- **Single screen**: Capture the primary monitor
- **All screens**: Capture the entire multi-monitor setup

### Annotation Tools
- **Arrow**: Draw arrows with adjustable thickness and color
- **Line**: Draw straight lines
- **Text**: Place text on screenshots
- **Numbering**: Sequential numbers for step-by-step instructions
- **Freehand**: Freehand drawing using a pen tool

### Customizable Properties
- **5 colors**: Red, Yellow, Green, Cyan, Purple
- **Variable thickness**: Adjustable from 1–12 pixels
- **Transparency**: 0.2–1.0 (20%–100%)

### Export
- **PNG**: Lossless format
- **JPEG**: Compressed format
- All annotations are permanently baked into the image

## 🚀 Quick Start

### Prerequisites
- Windows 10/11
- .NET 8.0 SDK with Windows Desktop workload

### Installation
```bash
git clone https://github.com/max2058/ScreenshotProMax
cd ScreenshotProMax
dotnet build
dotnet run --project ScreenshotProMax
```

## 📖 Usage

### Taking a Screenshot

1. **Hotkey method (recommended)**:
   - Press `Ctrl + D` anywhere
   - Drag a rectangle over the desired area
   - The main window opens automatically

2. **Button method**:
   - Click one of the capture buttons:
     - **Region**: Select a custom area
     - **Window**: Capture the active window
     - **Screen**: Capture the primary monitor
     - **All Screens**: Capture all monitors

### Adding Annotations
1. Select a tool (Arrow, Line, Text, Number, Freehand)
2. Choose color, thickness, and transparency
3. Click and drag on the screenshot:
   - **Arrow / Line**: Drag from start to end
   - **Number / Text**: Single click
   - **Freehand**: Drag to draw

### Saving
1. Click **Save**
2. Choose format (PNG / JPEG) and destination
3. All annotations are automatically merged into the image

## 🏗️ Project Structure

```
ScreenshotProMax/
├── Views/
│   ├── MainWindow.xaml/xaml.cs
│   └── RegionSelectorWindow.xaml/xaml.cs
├── ViewModels/
│   └── MainViewModel.cs
├── Models/
│   └── AnnotationModel.cs
├── Services/
│   ├── ScreenshotService.cs
│   ├── ImageExportService.cs
│   ├── HotkeyService.cs
│   └── NativeMethods.cs
├── Converters/
│   └── PointsToPointCollectionConverter.cs
└── Themes/
    └── Colors.xaml
```

## 🔧 Technology Stack

- **.NET 8.0** with WPF
- **MVVM pattern** using CommunityToolkit.Mvvm
- **Global hotkeys** via Windows API (user32.dll)
- **Multi-monitor support** via System.Windows.Forms.Screen
- **GDI+ screen capture** using System.Drawing

## 📝 Known Limitations
- Hotkey `Ctrl + D` only works while the application is running
- Windows-only (WPF + GDI+ dependencies)
- DPI awareness may require improvements on high-DPI displays

## 🛠️ Development

```bash
dotnet build
dotnet run --project ScreenshotProMax
dotnet clean
```

## 📄 License

This project is licensed under the **MIT License**.

## 🤝 Contributing

Contributions are welcome!  
Feel free to open an issue or submit a pull request.

## 📧 Contact

- GitHub: [@max2058](https://github.com/max2058)
- Repository: [ScreenshotProMax](https://github.com/max2058/ScreenshotProMax)
