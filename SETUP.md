# Setup

This project is a WPF text overlay editor with direct Spout sender output.

## Requirements

- Windows 10/11 x64
- .NET 8 SDK
- A Spout-compatible receiver app (OBS + Spout plugin, TouchDesigner, Resolume, etc.)

## Restore and Build

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

## Spout Quick Check

1. Open the app and click `Start Spout`.
2. Open your receiver and select sender `SpoutText`.
3. Edit text in-app and confirm live updates in the receiver.

## Notes

- The app renders with transparent background using `PixelFormats.Pbgra32`.
- Frames are converted to RGBA before Spout send.
- Sender initialization uses compatibility fallbacks when default mode is unavailable.
