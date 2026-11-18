using System;
using System.IO;
using System.Text.Json;
using ChangeAudioSource;
using System.Windows.Forms;
using Xunit;

namespace SoundShift.Tests;

public sealed class SettingsStoreTests
{
    [Fact]
    public void Load_ReturnsDefaults_WhenNoSettingsExist()
    {
        string root = CreateTempRoot();
        try
        {
            SettingsStore store = CreateStore(root);

            AppSettings settings = store.Load();

            Assert.Equal(Keys.Pause, settings.Key);
            Assert.Equal(0u, settings.Modifiers);
            Assert.False(settings.StartWithWindows);
            Assert.False(settings.CheckForUpdatesOnStartup);
            Assert.Empty(settings.ExcludedDeviceIds);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Load_IncludesAllDevices_WhenOlderSettingsHaveNoExclusions()
    {
        string root = CreateTempRoot();
        try
        {
            SettingsStore store = CreateStore(root);
            string settingsFile = Path.Combine(root, "current", "settings.json");
            Directory.CreateDirectory(Path.GetDirectoryName(settingsFile)!);
            File.WriteAllText(settingsFile, "{\"Key\": 19, \"StartWithWindows\": true}");

            AppSettings settings = store.Load();

            Assert.True(settings.StartWithWindows);
            Assert.Empty(settings.ExcludedDeviceIds);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Load_UsesLegacySettings_WhenCurrentSettingsAreMissing()
    {
        string root = CreateTempRoot();
        try
        {
            SettingsStore store = CreateStore(root);
            AppSettings expected = new()
            {
                Key = Keys.S,
                Modifiers = NativeMethods.ModControl | NativeMethods.ModAlt,
                StartWithWindows = true,
                CheckForUpdatesOnStartup = true,
                ExcludedDeviceIds = new() { "device-1", "device-2" }
            };

            string legacyFile = Path.Combine(root, "legacy", "settings.json");
            Directory.CreateDirectory(Path.GetDirectoryName(legacyFile)!);
            File.WriteAllText(legacyFile, JsonSerializer.Serialize(expected));

            AppSettings loaded = store.Load();

            Assert.Equal(expected.Key, loaded.Key);
            Assert.Equal(expected.Modifiers, loaded.Modifiers);
            Assert.Equal(expected.StartWithWindows, loaded.StartWithWindows);
            Assert.Equal(expected.CheckForUpdatesOnStartup, loaded.CheckForUpdatesOnStartup);
            Assert.Equal(expected.ExcludedDeviceIds, loaded.ExcludedDeviceIds);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Save_WritesSettingsFileToCurrentLocation()
    {
        string root = CreateTempRoot();
        try
        {
            SettingsStore store = CreateStore(root);
            AppSettings expected = new()
            {
                Key = Keys.F12,
                Modifiers = NativeMethods.ModControl,
                StartWithWindows = true,
                CheckForUpdatesOnStartup = true,
                ExcludedDeviceIds = new() { "device-1", "device-2" }
            };

            store.Save(expected);

            string savedFile = Path.Combine(root, "current", "settings.json");
            Assert.True(File.Exists(savedFile));

            AppSettings? saved = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(savedFile));
            Assert.NotNull(saved);
            Assert.Equal(expected.Key, saved!.Key);
            Assert.Equal(expected.Modifiers, saved.Modifiers);
            Assert.Equal(expected.StartWithWindows, saved.StartWithWindows);
            Assert.Equal(expected.CheckForUpdatesOnStartup, saved.CheckForUpdatesOnStartup);
            Assert.Equal(expected.ExcludedDeviceIds, saved.ExcludedDeviceIds);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static SettingsStore CreateStore(string root)
    {
        return new SettingsStore(
            Path.Combine(root, "current"),
            Path.Combine(root, "legacy"));
    }

    private static string CreateTempRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "SoundShift.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
