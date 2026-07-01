# SoundShift Installer

This folder contains the Inno Setup script used to package SoundShift for end users.

Build flow:

1. Publish the app self-contained with `scripts/publish-self-contained.ps1`.
2. Open `installer/SoundShift.iss` in Inno Setup.
3. Compile the script to create `dist/installer/SoundShift-Setup.exe`.

The installer installs the app to `C:\Program Files\SoundShift` by default and creates Start Menu shortcuts. An optional desktop icon is available during installation.
