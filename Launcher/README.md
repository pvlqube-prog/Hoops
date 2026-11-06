# QUBE 2K23 Launcher

A Windows desktop launcher that searches local drives for an existing NBA2K23.exe and can start the executable directly from its containing game folder.

## Features
- Scans common install locations and accessible fixed/removable drives (bounded directory depth).
- Lets the user choose the game folder if automatic detection does not find it.
- Verifies that NBA2K23.exe exists before enabling Play Now.
- Starts the local executable with its game directory as the working directory.
- Optional HTTP(S) health check for a separately hosted community service.
- Stores the selected executable path and optional status endpoint under the current user's LocalAppData folder.
- Does not download, patch, modify, replace, or bypass protections in game files.

## Important compatibility and ownership note
This launcher only starts the local executable. It does not bypass DRM, Steam ownership checks, authentication, or any other licensing control. If the installed copy requires Steam or another supported launcher, starting the executable directly may fail or still open that service. Use only a legitimate installation that you are authorized to run. This project does not provide compatibility with cracked or unauthorized copies.

NBA 2K23's official online services have been retired. The optional community endpoint check only reports the availability of a separate web service; it does not restore official 2K matchmaking or add multiplayer networking to the game.

## Build on Windows
Requirements: .NET 8 SDK and Windows.

```powershell
dotnet build .\Qube2K23Launcher\Qube2K23Launcher.csproj -c Release
dotnet publish .\Qube2K23Launcher\Qube2K23Launcher.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

The published executable is written under:
`Qube2K23Launcher\bin\Release\net8.0-windows\win-x64\publish\QUBE-2K23-Launcher.exe`
