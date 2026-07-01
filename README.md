# SoundShift

Small Windows system tray app that switches playback devices using a user-defined global hotkey.

![SoundShift Logo](assets/soundshift-logo.svg)

## Features

- Sits in the system tray after launch.
- Lets you bind a single key (example: `Pause`) or key combination (example: `Ctrl + Alt + S`).
- On hotkey press, cycles to the next active playback output device.
- Shows a tiny bottom-right popup with the newly active audio output.
- Optional Start with Windows toggle in settings.

## Requirements

- Windows 10 or 11
- .NET 9 SDK (or newer)

Check installed SDKs:

```powershell
dotnet --list-sdks
```

If this returns nothing, install SDK from:

- https://dotnet.microsoft.com/download

## Run

```powershell
dotnet restore
dotnet run
```

## Build

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

Published output goes to:

- `bin/Release/net9.0-windows/win-x64/publish/`

## Usage

1. Start the app.
2. Click the hotkey box and press your preferred key or combo.
3. Optionally enable **Start with Windows**.
4. Click **Save**.
5. Minimize or close the settings window (the app keeps running in tray).
6. Press your hotkey to switch audio output.

## Start Automatically On Boot

Enable the **Start with Windows** checkbox in the app and click **Save**.

SoundShift writes an auto-start entry under:

- `HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Run`

Tray menu:

- **Open Settings**
- **Exit**

## Notes

- If a hotkey is already used by another app, registration will fail and show a warning.
- Audio switching sets default device for Console, Multimedia, and Communications roles.
