using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenCut.Core;
using ScreenCut.Native;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace ScreenCut.Capture;

/// <summary>Grabs the entire virtual desktop (all monitors) in physical pixels.</summary>
internal static class ScreenCapturer
{
    private const double Dpi = 96;

    public static CaptureSnapshot Capture()
    {
        // Read the foreground window first — before anything of ours can take focus.
        var foreground = WindowEnumerator.GetForegroundWindowBounds();
        NativeMethods.GetCursorPos(out var cursor);
        var bounds = GetVirtualScreenBounds();

        var image = CaptureRegion(bounds);
        var windows = WindowEnumerator.GetVisibleWindows().Select(w => w.Offset(-bounds.X, -bounds.Y)).ToList();
        var monitors = Forms.Screen.AllScreens
            .Select(s => new PixelRect(s.Bounds.X - bounds.X, s.Bounds.Y - bounds.Y, s.Bounds.Width, s.Bounds.Height))
            .ToList();

        return new CaptureSnapshot(
            image,
            bounds,
            windows,
            monitors,
            foreground?.Offset(-bounds.X, -bounds.Y),
            new Point(cursor.X - bounds.X, cursor.Y - bounds.Y));
    }

    private static PixelRect GetVirtualScreenBounds() => new(
        NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN),
        NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN),
        NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN),
        NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN));

    private static BitmapSource CaptureRegion(PixelRect bounds)
    {
        using var bitmap = new Drawing.Bitmap(bounds.Width, bounds.Height, Drawing.Imaging.PixelFormat.Format32bppRgb);
        using (var graphics = Drawing.Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, new Drawing.Size(bounds.Width, bounds.Height), Drawing.CopyPixelOperation.SourceCopy);
        }

        var data = bitmap.LockBits(new Drawing.Rectangle(0, 0, bounds.Width, bounds.Height), Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
        try
        {
            var source = BitmapSource.Create(
                bounds.Width, bounds.Height, Dpi, Dpi, PixelFormats.Bgr32, null, data.Scan0, data.Stride * bounds.Height, data.Stride);
            source.Freeze();
            return source;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}
