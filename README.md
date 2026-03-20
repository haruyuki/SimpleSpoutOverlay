# SpoutText

A WPF desktop app for building transparent text overlays with multiple editable layers.

The app currently focuses on a reliable editor + live preview workflow. Spout output has been removed for now, but the renderer keeps an alpha-safe pipeline so output adapters (like Spout) can be added later.

## Features

- Multiple text layers with add/delete/reorder
- Per-layer text, font family, size, fill color, outline, position, and scale
- Transparent preview rendering (`Pbgra32`)
- Geometry-based text rendering (`FormattedText` -> `Geometry`) with proper outline drawing
- WPF color picker dialog for fill/outline colors

## Project Structure

- `SpoutText/MainWindow.xaml` - app UI layout
- `SpoutText/MainWindow.xaml.cs` - window logic and color picker wiring
- `SpoutText/Models/TextLayer.cs` - layer model
- `SpoutText/Models/LayerManager.cs` - layer collection/order/selection management
- `SpoutText/UI/ViewModels/MainWindowViewModel.cs` - MVVM state + commands
- `SpoutText/Rendering/TextLayerRenderer.cs` - transparent bitmap renderer

## Build

```powershell
cd C:\Users\Haru\RiderProjects\SpoutText

dotnet restore

dotnet build -c Debug
```

## Run

```powershell
cd C:\Users\Haru\RiderProjects\SpoutText

dotnet run --project SpoutText
```

## Transparency Note (Future Output Ready)

The preview is rendered to `RenderTargetBitmap` using `PixelFormats.Pbgra32` and a transparent background. This keeps alpha information intact and makes it straightforward to reintroduce output backends (for example, Spout) later.

---

**Happy streaming! 🎥✨**
