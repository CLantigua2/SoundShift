using System;
using System.IO;
using System.Text.Json;

namespace ChangeAudioSource;

internal sealed class SettingsStore
{
    private const string CurrentFolderName = "SoundShift";
    private const string LegacyFolderName = "ChangeAudioSource";

    private static readonly SettingsStore Default = CreateDefault();

    private readonly string settingsPath;
    private readonly string legacySettingsPath;

    internal SettingsStore(string settingsFolder, string legacySettingsFolder)
    {
        settingsPath = Path.Combine(settingsFolder, "settings.json");
        legacySettingsPath = Path.Combine(legacySettingsFolder, "settings.json");
    }

    internal static AppSettings LoadDefault() => Default.Load();

    internal static void SaveDefault(AppSettings settings) => Default.Save(settings);

    internal static SettingsStore CreateDefault()
    {
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return new SettingsStore(
            Path.Combine(appDataFolder, CurrentFolderName),
            Path.Combine(appDataFolder, LegacyFolderName));
    }

    internal AppSettings Load()
    {
        try
        {
            if (File.Exists(settingsPath))
            {
                string json = File.ReadAllText(settingsPath);
                AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }

            if (File.Exists(legacySettingsPath))
            {
                string legacyJson = File.ReadAllText(legacySettingsPath);
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

    internal void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(settingsPath, json);
    }
}
