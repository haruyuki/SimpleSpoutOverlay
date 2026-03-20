# SpoutText

SpoutText is a WPF desktop app for building transparent text overlays and sending them directly to a Spout output.

The editor preview and Spout output both use the same alpha-safe render pipeline, so what you see in the canvas is what gets sent.

## Current Functionality

- Multi-layer text editor (add/delete/reorder)
- Per-layer text controls: font, size, fill, outline, position, and scale
- Live preview at `1920x1080`
- Spout sender output (toggle from the Preview panel)
- Transparent rendering pipeline (`PixelFormats.Pbgra32` -> RGBA for Spout)
- Fallback sender initialization for broader machine compatibility

## Requirements

- Windows 10/11 x64
- .NET 8 SDK
- A Spout-compatible receiver app (for example OBS with Spout plugin, TouchDesigner, Resolume, etc.)

## Build

```powershell
cd C:\Users\Haru\RiderProjects\SpoutText
dotnet restore
dotnet build -c Debug
```

## Run

```powershell
cd C:\Users\Haru\RiderProjects\SpoutText
dotnet run --project .\SpoutText\SpoutText.csproj
```

## Using Spout Output

1. Launch the app.
2. Create/edit text layers.
3. Click `Start Spout` in the Preview panel.
4. In your receiver app, select sender name `SpoutText`.

If default sender creation is not available on a machine, the app automatically retries with compatibility modes.

## Project Structure

- `SpoutText/MainWindow.xaml` - app UI layout and Spout toggle button
- `SpoutText/MainWindow.xaml.cs` - window logic and view model disposal
- `SpoutText/UI/ViewModels/MainWindowViewModel.cs` - MVVM state, preview updates, Spout control
- `SpoutText/Rendering/TextLayerRenderer.cs` - transparent text rendering
- `SpoutText/Rendering/BitmapConverter.cs` - PBGRA32 to RGBA conversion
- `SpoutText/Rendering/SpoutOutputManager.cs` - Spout sender lifecycle and frame send

---

For deeper implementation details, see `SPOUT_IMPLEMENTATION.md` and `SPOUT_USAGE_GUIDE.md`.
