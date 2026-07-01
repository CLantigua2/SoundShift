using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ChangeAudioSource;

internal sealed class MainForm : Form
{
    private const int HotkeyId = 0xB001;

    private readonly AudioDeviceService audioDeviceService = new();
    private readonly NotifyIcon trayIcon;
    private readonly Icon appIcon;
    private readonly TextBox hotkeyTextBox;
    private readonly Label currentHotkeyLabel;
    private readonly CheckBox startWithWindowsCheckBox;

    private AppSettings settings;
    private Keys pendingKey;
    private uint pendingModifiers;
    private bool isExiting;

    internal MainForm()
    {
        settings = SettingsStore.Load();
        pendingKey = settings.Key;
        pendingModifiers = settings.Modifiers;

        appIcon = Branding.CreateAppIcon();

        Text = Branding.AppDisplayName;
        Icon = appIcon;
        ClientSize = new Size(330, 175);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;

        Label instructionsLabel = new()
        {
            Text = "Click the box and press any key combo (or one key).",
            AutoSize = true,
            Location = new Point(12, 14)
        };

        hotkeyTextBox = new TextBox
        {
            ReadOnly = true,
            Location = new Point(12, 42),
            Width = 300,
            TabStop = true,
            Text = FormatHotkey(pendingModifiers, pendingKey)
        };
        hotkeyTextBox.KeyDown += HotkeyTextBox_KeyDown;

        Button saveButton = new()
        {
            Text = "Save",
            Location = new Point(12, 74),
            Width = 80
        };
        saveButton.Click += SaveButton_Click;

        currentHotkeyLabel = new Label
        {
            AutoSize = true,
            Location = new Point(110, 79),
            Text = "Active: " + FormatHotkey(settings.Modifiers, settings.Key)
        };

        startWithWindowsCheckBox = new CheckBox
        {
            AutoSize = true,
            Location = new Point(12, 112),
            Text = "Start with Windows",
            Checked = settings.StartWithWindows
        };

        Controls.Add(instructionsLabel);
        Controls.Add(hotkeyTextBox);
        Controls.Add(saveButton);
        Controls.Add(currentHotkeyLabel);
        Controls.Add(startWithWindowsCheckBox);

        ContextMenuStrip trayMenu = new();
        trayMenu.Items.Add("Open Settings", null, (_, _) => ShowSettings());
        trayMenu.Items.Add("Exit", null, (_, _) => ExitApplication());

        trayIcon = new NotifyIcon
        {
            Icon = appIcon,
            Visible = true,
            Text = Branding.AppDisplayName,
            ContextMenuStrip = trayMenu
        };

        trayIcon.DoubleClick += (_, _) => ShowSettings();

        Resize += MainForm_Resize;
        FormClosing += MainForm_FormClosing;

        _ = StartupManager.SetEnabled(settings.StartWithWindows);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        RegisterCurrentHotkey();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WmHotkey && m.WParam == HotkeyId)
        {
            SwitchToNextPlaybackDevice();
        }

        base.WndProc(ref m);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            NativeMethods.UnregisterHotKey(Handle, HotkeyId);
            trayIcon.Visible = false;
            trayIcon.Dispose();
            appIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    private void HotkeyTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        e.SuppressKeyPress = true;

        Keys key = e.KeyCode;
        if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu || key == Keys.LWin || key == Keys.RWin)
        {
            return;
        }

        uint modifiers = 0;
        if (e.Control)
        {
            modifiers |= NativeMethods.ModControl;
        }

        if (e.Alt)
        {
            modifiers |= NativeMethods.ModAlt;
        }

        if (e.Shift)
        {
            modifiers |= NativeMethods.ModShift;
        }

        if ((Control.ModifierKeys & Keys.LWin) == Keys.LWin || (Control.ModifierKeys & Keys.RWin) == Keys.RWin)
        {
            modifiers |= NativeMethods.ModWin;
        }

        pendingKey = key;
        pendingModifiers = modifiers;
        hotkeyTextBox.Text = FormatHotkey(pendingModifiers, pendingKey);
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        settings = new AppSettings
        {
            Key = pendingKey,
            Modifiers = pendingModifiers,
            StartWithWindows = startWithWindowsCheckBox.Checked
        };

        bool startupUpdated = StartupManager.SetEnabled(settings.StartWithWindows);
        SettingsStore.Save(settings);
        RegisterCurrentHotkey();
        currentHotkeyLabel.Text = "Active: " + FormatHotkey(settings.Modifiers, settings.Key);

        if (!startupUpdated)
        {
            MessageBox.Show(
                "Could not update startup setting.",
                "Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void RegisterCurrentHotkey()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(Handle, HotkeyId);

        uint modifiers = settings.Modifiers | NativeMethods.ModNoRepeat;
        bool ok = NativeMethods.RegisterHotKey(Handle, HotkeyId, modifiers, (uint)settings.Key);
        if (!ok)
        {
            MessageBox.Show(
                "Could not register this hotkey. It may already be in use.",
                "Hotkey Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void SwitchToNextPlaybackDevice()
    {
        var devices = audioDeviceService.GetActivePlaybackDevices();
        if (devices.Count == 0)
        {
            ShowPopup("No active playback devices found");
            return;
        }

        string? currentId = audioDeviceService.GetDefaultPlaybackDeviceId();
        int currentIndex = devices.FindIndex(device => device.Id == currentId);

        int nextIndex = currentIndex < 0
            ? 0
            : (currentIndex + 1) % devices.Count;

        var nextDevice = devices[nextIndex];

        try
        {
            audioDeviceService.SetDefaultPlaybackDevice(nextDevice.Id);
            ShowPopup("Audio Output: " + nextDevice.Name);
        }
        catch (Exception)
        {
            ShowPopup("Could not switch audio output");
        }
    }

    private static string FormatHotkey(uint modifiers, Keys key)
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

    private void ShowPopup(string message)
    {
        NotificationForm popup = new(message);
        popup.FormClosed += (_, _) => popup.Dispose();
        popup.Show();
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!isExiting)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void ShowSettings()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        isExiting = true;
        Close();
    }
}
