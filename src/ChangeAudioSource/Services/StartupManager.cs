using System;
using Microsoft.Win32;
using System.Windows.Forms;

namespace ChangeAudioSource;

internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "SoundShift";

    internal static bool IsEnabled()
    {
        using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return runKey?.GetValue(StartupValueName) is string;
    }

    internal static bool SetEnabled(bool enabled)
    {
        try
        {
            using RegistryKey? runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (runKey is null)
            {
                return false;
            }

            if (enabled)
            {
                string quotedPath = '"' + Application.ExecutablePath + '"';
                runKey.SetValue(StartupValueName, quotedPath, RegistryValueKind.String);
            }
            else
            {
                runKey.DeleteValue(StartupValueName, throwOnMissingValue: false);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
