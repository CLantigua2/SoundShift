using System.Linq;
using System.Windows.Forms;

namespace ChangeAudioSource;

internal static class HotkeyDisplayFormatter
{
    internal static string FormatHotkey(uint modifiers, Keys key)
    {
        string[] parts =
        {
            (modifiers & NativeMethods.ModControl) != 0 ? "Ctrl" : string.Empty,
            (modifiers & NativeMethods.ModAlt) != 0 ? "Alt" : string.Empty,
            (modifiers & NativeMethods.ModShift) != 0 ? "Shift" : string.Empty,
            (modifiers & NativeMethods.ModWin) != 0 ? "Win" : string.Empty,
            key.ToString()
        };

        return string.Join(" + ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
