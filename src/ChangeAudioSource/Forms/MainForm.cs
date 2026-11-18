using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;

namespace ChangeAudioSource;

internal sealed class MainForm : Form
{
    private readonly AudioDeviceService audioDeviceService = new();
    private readonly GlobalHotkey hotkey = new();
    private readonly NotifyIcon trayIcon;
    private readonly Icon appIcon;
    private readonly TextBox hotkeyTextBox;
    private readonly Label currentHotkeyLabel;
    private readonly CheckBox startWithWindowsCheckBox;
    private readonly CheckBox checkForUpdatesCheckBox;
    private readonly CheckedListBox deviceList;
    private readonly HashSet<string> pendingExcludedDeviceIds;
    private readonly HttpClient updateClient = new() { Timeout = TimeSpan.FromMinutes(5) };
    private readonly CancellationTokenSource updateCancellation = new();

    private AppSettings settings;
    private Keys pendingKey;
    private uint pendingModifiers;
    private bool isExiting;
    private bool checkingForUpdates;

    internal MainForm()
    {
        settings = SettingsStore.LoadDefault();
        pendingKey = settings.Key;
        pendingModifiers = settings.Modifiers;
        pendingExcludedDeviceIds = new HashSet<string>(settings.ExcludedDeviceIds ?? new(), StringComparer.OrdinalIgnoreCase);

        appIcon = Branding.CreateAppIcon();

        Text = Branding.AppDisplayName;
        Icon = appIcon;
        ClientSize = new Size(330, 380);
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
            Text = HotkeyDisplayFormatter.FormatHotkey(pendingModifiers, pendingKey)
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
            Text = "Active: " + HotkeyDisplayFormatter.FormatHotkey(settings.Modifiers, settings.Key)
        };

        startWithWindowsCheckBox = new CheckBox
        {
            AutoSize = true,
            Location = new Point(12, 112),
            Text = "Start with Windows",
            Checked = settings.StartWithWindows
        };

        checkForUpdatesCheckBox = new CheckBox
        {
            AutoSize = true,
            Location = new Point(12, 140),
            Text = "Check for updates on startup",
            Checked = settings.CheckForUpdatesOnStartup
        };

        Label deviceLabel = new()
        {
            AutoSize = true,
            Location = new Point(12, 179),
            Text = "Playback devices to cycle"
        };

        deviceList = new CheckedListBox
        {
            Location = new Point(12, 203),
            Size = new Size(300, 128),
            CheckOnClick = true,
            DisplayMember = nameof(AudioDeviceService.PlaybackDevice.Name),
            HorizontalScrollbar = true
        };
        deviceList.ItemCheck += DeviceList_ItemCheck;

        Button refreshButton = new()
        {
            Text = "Refresh devices",
            Location = new Point(12, 342),
            AutoSize = true
        };
        refreshButton.Click += (_, _) => RefreshDevices();

        Controls.Add(instructionsLabel);
        Controls.Add(hotkeyTextBox);
        Controls.Add(saveButton);
        Controls.Add(currentHotkeyLabel);
        Controls.Add(startWithWindowsCheckBox);
        Controls.Add(checkForUpdatesCheckBox);
        Controls.Add(deviceLabel);
        Controls.Add(deviceList);
        Controls.Add(refreshButton);

        ContextMenuStrip trayMenu = new();
        trayMenu.Items.Add("Open Settings", null, (_, _) => ShowSettings());
        trayMenu.Items.Add("Check for updates", null, async (_, _) => await CheckForUpdatesAsync(manual: true));
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
        Shown += async (_, _) =>
        {
            if (settings.CheckForUpdatesOnStartup)
            {
                await CheckForUpdatesAsync(manual: false);
            }
        };

        _ = StartupManager.SetEnabled(settings.StartWithWindows);
        RefreshDevices();
        hotkey.Pressed += (_, _) => SwitchToNextPlaybackDevice();
        RegisterCurrentHotkey();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            updateCancellation.Cancel();
            updateCancellation.Dispose();
            updateClient.Dispose();
            hotkey.Dispose();
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
        hotkeyTextBox.Text = HotkeyDisplayFormatter.FormatHotkey(pendingModifiers, pendingKey);
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        settings = new AppSettings
        {
            Key = pendingKey,
            Modifiers = pendingModifiers,
            StartWithWindows = startWithWindowsCheckBox.Checked,
            CheckForUpdatesOnStartup = checkForUpdatesCheckBox.Checked,
            ExcludedDeviceIds = pendingExcludedDeviceIds.ToList()
        };

        bool startupUpdated = StartupManager.SetEnabled(settings.StartWithWindows);
        SettingsStore.SaveDefault(settings);
        RegisterCurrentHotkey();
        currentHotkeyLabel.Text = "Active: " + HotkeyDisplayFormatter.FormatHotkey(settings.Modifiers, settings.Key);

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
        bool ok = hotkey.Register(settings.Key, settings.Modifiers);
        if (!ok)
        {
            MessageBox.Show(
                "Could not register this hotkey. It may already be in use.",
                "Hotkey Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private async Task CheckForUpdatesAsync(bool manual)
    {
        if (checkingForUpdates)
        {
            return;
        }

        checkingForUpdates = true;
        try
        {
            UpdateService updater = new(updateClient);
            Version assemblyVersion = typeof(MainForm).Assembly.GetName().Version ?? new Version(1, 0, 0);
            Version currentVersion = new(assemblyVersion.Major, assemblyVersion.Minor, Math.Max(0, assemblyVersion.Build));
            using CancellationTokenSource checkTimeout = CancellationTokenSource.CreateLinkedTokenSource(updateCancellation.Token);
            checkTimeout.CancelAfter(TimeSpan.FromSeconds(15));
            UpdateService.UpdateInfo? update = await updater.CheckAsync(currentVersion, checkTimeout.Token);
            if (update is null)
            {
                if (manual && !isExiting)
                {
                    MessageBox.Show("SoundShift is up to date.", Branding.AppDisplayName);
                }
                return;
            }

            if (isExiting || MessageBox.Show(
                $"SoundShift {update.Version} is available. Download and run the installer?",
                Branding.AppDisplayName, MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
            {
                return;
            }

            trayIcon.ShowBalloonTip(3000, Branding.AppDisplayName, "Downloading update...", ToolTipIcon.Info);
            string installerPath = await updater.DownloadAsync(update, updateCancellation.Token);
            if (isExiting)
            {
                return;
            }

            using Process? installer = Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
            if (installer is null)
            {
                throw new InvalidOperationException("Could not start the installer.");
            }
            ExitApplication();
        }
        catch (OperationCanceledException) when (isExiting)
        {
        }
        catch (Exception)
        {
            if (manual && !isExiting)
            {
                MessageBox.Show("Could not check for or install updates. Please try again later.",
                    Branding.AppDisplayName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (!isExiting)
            {
                trayIcon.ShowBalloonTip(3000, Branding.AppDisplayName,
                    "Could not check for or install updates.", ToolTipIcon.Warning);
            }
        }
        finally
        {
            checkingForUpdates = false;
        }
    }

    private void SwitchToNextPlaybackDevice()
    {
        try
        {
            var devices = audioDeviceService.GetActivePlaybackDevices();
            if (devices.Count == 0)
            {
                ShowPopup("No active playback devices found");
                return;
            }

            var nextDevice = AudioDeviceService.GetNextPlaybackDevice(
                devices, settings.ExcludedDeviceIds ?? new(), audioDeviceService.GetDefaultPlaybackDeviceId());
            if (nextDevice is null)
            {
                ShowPopup("No playback devices selected");
                return;
            }

            audioDeviceService.SetDefaultPlaybackDevice(nextDevice.Id);
            ShowPopup("Audio Output: " + nextDevice.Name);
        }
        catch (Exception)
        {
            ShowPopup("Could not switch audio output");
        }
    }

    private void DeviceList_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        var device = (AudioDeviceService.PlaybackDevice)deviceList.Items[e.Index];
        if (e.NewValue == CheckState.Checked)
        {
            pendingExcludedDeviceIds.Remove(device.Id);
        }
        else
        {
            pendingExcludedDeviceIds.Add(device.Id);
        }
    }

    private void RefreshDevices()
    {
        try
        {
            var devices = audioDeviceService.GetActivePlaybackDevices();
            deviceList.ItemCheck -= DeviceList_ItemCheck;
            try
            {
                deviceList.Items.Clear();
                foreach (var device in devices)
                {
                    deviceList.Items.Add(device, !pendingExcludedDeviceIds.Contains(device.Id));
                }
            }
            finally
            {
                deviceList.ItemCheck += DeviceList_ItemCheck;
            }
        }
        catch (Exception)
        {
            MessageBox.Show("Could not load playback devices.", Branding.AppDisplayName,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
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
        RefreshDevices();
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
