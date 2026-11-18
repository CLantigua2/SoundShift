using System.Windows.Forms;

namespace ChangeAudioSource;

internal sealed class GlobalHotkey : NativeWindow, IDisposable
{
    private const int HotkeyId = 0xB001;
    private bool registered;

    internal GlobalHotkey()
    {
        CreateHandle(new CreateParams { Parent = new IntPtr(-3) });
    }

    internal event EventHandler? Pressed;

    internal bool Register(Keys key, uint modifiers)
    {
        if (registered)
        {
            NativeMethods.UnregisterHotKey(Handle, HotkeyId);
            registered = false;
        }

        registered = NativeMethods.RegisterHotKey(Handle, HotkeyId, modifiers | NativeMethods.ModNoRepeat, (uint)key);
        return registered;
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeMethods.WmHotkey && message.WParam == HotkeyId)
        {
            Pressed?.Invoke(this, EventArgs.Empty);
        }

        base.WndProc(ref message);
    }

    public void Dispose()
    {
        if (Handle == IntPtr.Zero)
        {
            return;
        }

        if (registered)
        {
            NativeMethods.UnregisterHotKey(Handle, HotkeyId);
            registered = false;
        }

        DestroyHandle();
    }
}