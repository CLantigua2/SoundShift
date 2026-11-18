using System.Runtime.InteropServices;
using System.Windows.Forms;
using ChangeAudioSource;
using Xunit;

namespace SoundShift.Tests;

public sealed class GlobalHotkeyTests
{
    [Fact]
    public async Task HotkeyRemainsRegisteredAndReceivesMessagesAfterFormIsHiddenAndClosed()
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Thread thread = new(() =>
        {
            try
            {
                using GlobalHotkey hotkey = new();
                using Form settingsForm = new();
                using Form competingWindow = new();
                _ = settingsForm.Handle;
                _ = competingWindow.Handle;
                const uint modifiers = NativeMethods.ModControl | NativeMethods.ModAlt | NativeMethods.ModShift;
                Assert.True(hotkey.Register(Keys.F24, modifiers));

                int presses = 0;
                hotkey.Pressed += (_, _) => presses++;
                settingsForm.Hide();
                settingsForm.Close();

                bool competingRegistration = NativeMethods.RegisterHotKey(competingWindow.Handle, 1, modifiers, (uint)Keys.F24);
                if (competingRegistration)
                {
                    NativeMethods.UnregisterHotKey(competingWindow.Handle, 1);
                }
                Assert.False(competingRegistration);
                SendMessage(hotkey.Handle, NativeMethods.WmHotkey, new IntPtr(2), IntPtr.Zero);
                Assert.Equal(0, presses);
                SendMessage(hotkey.Handle, NativeMethods.WmHotkey, new IntPtr(0xB001), IntPtr.Zero);
                Assert.Equal(1, presses);

                hotkey.Dispose();
                bool releasedRegistration = NativeMethods.RegisterHotKey(competingWindow.Handle, 1, modifiers, (uint)Keys.F24);
                if (releasedRegistration)
                {
                    NativeMethods.UnregisterHotKey(competingWindow.Handle, 1);
                }
                Assert.True(releasedRegistration);

                completion.SetResult();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await completion.Task;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
}