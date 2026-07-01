using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChangeAudioSource;

internal sealed class NotificationForm : Form
{
    private readonly System.Windows.Forms.Timer closeTimer;
    private readonly Label messageLabel;

    internal NotificationForm(string message)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Color.FromArgb(24, 24, 24);
        Size = new Size(340, 64);

        messageLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 14, 0),
            Text = message
        };

        Controls.Add(messageLabel);

        closeTimer = new System.Windows.Forms.Timer { Interval = 1300 };
        closeTimer.Tick += (_, _) => Close();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        PositionBottomRight();
        closeTimer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            closeTimer.Dispose();
            messageLabel.Dispose();
        }

        base.Dispose(disposing);
    }

    private void PositionBottomRight()
    {
        Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;
        int x = area.Right - Width - 14;
        int y = area.Bottom - Height - 14;
        Location = new Point(x, y);
    }
}
