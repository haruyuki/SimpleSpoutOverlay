# Setup

This project is now a standalone WPF text overlay editor (Spout runtime removed for now).

## Requirements

- Windows 10/11 x64
- .NET 9 SDK

## Restore and Build

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

## Notes

- Preview rendering stays alpha-safe with transparent background (`Pbgra32`).
- This keeps the editor ready for a future output adapter layer (such as Spout) without changing core editing behavior.
