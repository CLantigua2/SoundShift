using System.Windows.Forms;

namespace ChangeAudioSource;

internal sealed class AppSettings
{
    public Keys Key { get; set; } = Keys.Pause;

    public uint Modifiers { get; set; }

    public bool StartWithWindows { get; set; }

    public bool CheckForUpdatesOnStartup { get; set; }

    public List<string> ExcludedDeviceIds { get; set; } = new();
}
