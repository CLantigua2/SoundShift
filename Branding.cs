using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ChangeAudioSource;

internal static class Branding
{
    internal const string AppDisplayName = "SoundShift";

    internal static Icon CreateAppIcon()
    {
        using Bitmap canvas = new(64, 64, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(canvas))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            Rectangle circleBounds = new(4, 4, 56, 56);
            using GraphicsPath circlePath = new();
            circlePath.AddEllipse(circleBounds);

            using LinearGradientBrush fill = new(
                new Point(4, 4),
                new Point(60, 60),
                Color.FromArgb(0, 148, 255),
                Color.FromArgb(0, 214, 143));

            g.FillPath(fill, circlePath);

            using Pen ring = new(Color.FromArgb(220, 255, 255, 255), 3f);
            g.DrawPath(ring, circlePath);

            using Pen wavePen = new(Color.FromArgb(240, 255, 255, 255), 4f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            PointF[] wave =
            {
                new(16, 34),
                new(24, 24),
                new(32, 40),
                new(40, 26),
                new(48, 34)
            };
            g.DrawLines(wavePen, wave);
        }

        IntPtr handle = canvas.GetHicon();
        try
        {
            using Icon tmp = Icon.FromHandle(handle);
            return (Icon)tmp.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }
}
