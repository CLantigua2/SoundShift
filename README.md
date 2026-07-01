# SoundShift

Small Windows system tray app that switches playback devices using a user-defined global hotkey.

![SoundShift Logo](assets/soundshift-logo.svg)

## Install

1. Open the [GitHub Releases page](https://github.com/CLantigua2/SoundShift/releases).
2. Download the latest `SoundShift-Setup.exe` installer.
3. Run the installer and follow the prompts.

The installer places SoundShift in `C:\Program Files\SoundShift`, adds Start Menu shortcuts, and can optionally create a desktop shortcut.

## Usage

1. Start SoundShift from the Start Menu or desktop shortcut.
2. Open the settings window from the tray icon.
3. Click the hotkey box and press the key or key combination you want to use.
4. Optionally enable **Start with Windows**.
5. Click **Save**.
6. Minimize or close the settings window. SoundShift stays running in the system tray.
7. Press your hotkey to switch to the next active playback device.

## Features

- Sits in the system tray after launch.
- Lets you bind a single key, such as `Pause`, or a key combination, such as `Ctrl + Alt + S`.
- Cycles to the next active playback output device when the hotkey is pressed.
- Shows a small bottom-right popup with the new audio output.
- Includes an optional **Start with Windows** setting.

## Requirements

- Windows 10 or 11

You do not need the .NET SDK to use the installer from GitHub Releases.

## For Developers

If you want to run or build SoundShift from source, install the .NET 9 SDK or newer.

Check installed SDKs:

```powershell
dotnet --list-sdks
```

If this returns nothing, install the SDK from:

- https://dotnet.microsoft.com/download

Run from source:

```powershell
dotnet restore
dotnet run
```

Run tests:

```powershell
dotnet test tests/SoundShift.Tests/SoundShift.Tests.csproj
```

Build a local publish from source:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

The GitHub Actions workflow publishes the app self-contained and builds the installer `.exe` automatically for tagged releases.

## Project Structure

- `src/ChangeAudioSource/` contains the app source code.
- `tests/SoundShift.Tests/` contains the unit tests.

## Start Automatically On Boot

Enable the **Start with Windows** checkbox in the app and click **Save**.

SoundShift writes an auto-start entry under:

- `HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Run`

Tray menu:

- **Open Settings**
- **Exit**

## Notes

- If a hotkey is already used by another app, registration will fail and a warning will appear.
- Audio switching sets the default device for Console, Multimedia, and Communications roles.
