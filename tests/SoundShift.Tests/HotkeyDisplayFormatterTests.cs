using System.Windows.Forms;
using ChangeAudioSource;
using Xunit;

namespace SoundShift.Tests;

public sealed class HotkeyDisplayFormatterTests
{
    [Fact]
    public void FormatsSingleKeyHotkey()
    {
        string hotkey = HotkeyDisplayFormatter.FormatHotkey(0, Keys.Pause);

        Assert.Equal("Pause", hotkey);
    }

    [Fact]
    public void FormatsModifierCombinationInConsistentOrder()
    {
        uint modifiers = NativeMethods.ModControl | NativeMethods.ModAlt | NativeMethods.ModShift;

        string hotkey = HotkeyDisplayFormatter.FormatHotkey(modifiers, Keys.S);

        Assert.Equal("Ctrl + Alt + Shift + S", hotkey);
    }
}
