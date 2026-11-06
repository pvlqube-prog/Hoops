# QUBE 2K23 Launcher

A Windows desktop launcher for a **legitimate Steam installation** of NBA 2K23.

## What it does
- Opens NBA 2K23 through Steam's official app launch URI (Steam App ID: 1644960).
- Does not inspect, modify, patch, replace, or bypass the game's executable or Steam licensing.
- Optionally checks an independently hosted QUBE community service's HTTP(S) health endpoint.
- Saves the optional endpoint locally under the current Windows user's LocalAppData folder.

## Important online-service limitation
NBA 2K23's original 2K online services have been retired. The community status field only checks a separate QUBE-operated web service. It does **not** restore official 2K matchmaking or provide gameplay networking. Any future community multiplayer integration would require a separately designed service and lawful compatibility with the game; the launcher alone cannot add multiplayer to NBA 2K23.

## Build on Windows
Requirements: .NET 8 SDK and Windows.

```powershell
dotnet build .\Qube2K23Launcher\Qube2K23Launcher.csproj -c Release
dotnet publish .\Qube2K23Launcher\Qube2K23Launcher.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

The published executable is written under:
`Qube2K23Launcher\bin\Release\net8.0-windows\win-x64\publish\QUBE-2K23-Launcher.exe`

## Run
Install Steam, sign in to an account that owns NBA 2K23, then open the launcher and select **LAUNCH NBA 2K23**. Steam handles ownership and starts the game if it is available to that account.
