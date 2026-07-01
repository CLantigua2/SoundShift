using System;
using System.IO;
using System.Text.Json;

namespace ChangeAudioSource;

internal static class SettingsStore
{
    private const string CurrentFolderName = "SoundShift";
    private const string LegacyFolderName = "ChangeAudioSource";

    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        CurrentFolderName);

    private static readonly string LegacySettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        LegacyFolderName,
        "settings.json");

    private static readonly string SettingsPath = Path.Combine(SettingsFolder, "settings.json");

    internal static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }

            if (File.Exists(LegacySettingsPath))
            {
                string legacyJson = File.ReadAllText(LegacySettingsPath);
                AppSettings? legacySettings = JsonSerializer.Deserialize<AppSettings>(legacyJson);
                return legacySettings ?? new AppSettings();
            }

            return new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    internal static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsFolder);
        string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}
