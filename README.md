# SpoutText

SpoutText is a WPF desktop app for building transparent text overlays and sending them directly to Spout.

The editor preview and Spout output use the same alpha-safe render pipeline, so what you see in the canvas is what gets sent.

## Features

- Multi-layer text workflow (add, delete, move up/down, drag-and-drop reorder)
- Per-layer controls: text, font family, size, fill color, outline toggle/color/thickness, position, and scale
- Live `1920x1080` preview with transparent background
- Undo/redo history for layer operations and property changes
- Slider gesture-aware undo (one drag action = one undo step)
- Save/load setup as JSON session files
- Auto-load default session on startup and auto-save on app close
- Spout sender output with start/stop toggle
- Spout sender fallback initialization for better compatibility across machines

## Keyboard Shortcuts

- `Ctrl+Z` - Undo
- `Ctrl+Y` - Redo
- `Ctrl+S` - Save setup
- `Ctrl+O` - Load setup
- `Ctrl+N` - Add layer
- `Ctrl+Delete` - Delete selected layer
- `Ctrl+Up` - Move selected layer up (when focus is in the layer list)
- `Ctrl+Down` - Move selected layer down (when focus is in the layer list)

## Requirements

- Windows 10/11 x64
- .NET 8 SDK

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

## Session Files

- Manual save/load uses JSON setup files via the Session buttons or shortcuts.
- Default session path: `%AppData%\SpoutText\session.json`.
- Setup files store layers and selection state.

## Using Spout Output

1. Launch the app.
2. Create or edit one or more text layers.
3. Click `Start Spout` in the top Session/Spout bar.
4. In your receiver app, select sender name `SpoutText`.

## Setup Notes

For environment and dependency setup details, see `SETUP.md`.
