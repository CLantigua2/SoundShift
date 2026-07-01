using System.Windows.Forms;

namespace ChangeAudioSource;

internal sealed class AppSettings
{
    public Keys Key { get; set; } = Keys.Pause;

    public uint Modifiers { get; set; }

    public bool StartWithWindows { get; set; }
}
