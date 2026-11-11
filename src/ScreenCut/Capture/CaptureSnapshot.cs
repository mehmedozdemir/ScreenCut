using System.Windows;
using System.Windows.Media.Imaging;
using ScreenCut.Core;

namespace ScreenCut.Capture;

/// <summary>
/// A frozen screenshot of the whole virtual desktop plus the geometry needed for selection.
/// All rectangles except <see cref="VirtualBounds"/> are relative to the virtual desktop's top-left corner.
/// </summary>
internal sealed record CaptureSnapshot(
    BitmapSource Image,
    PixelRect VirtualBounds,
    IReadOnlyList<PixelRect> Windows,
    IReadOnlyList<PixelRect> Monitors,
    PixelRect? ForegroundWindow,
    Point Cursor)
{
    public Rect ImageBounds => new(0, 0, Image.PixelWidth, Image.PixelHeight);

    public PixelRect MonitorAt(Point point)
    {
        foreach (var monitor in Monitors)
        {
            if (monitor.Contains(point.X, point.Y))
            {
                return monitor;
            }
        }

        return Monitors.Count > 0 ? Monitors[0] : new PixelRect(0, 0, Image.PixelWidth, Image.PixelHeight);
    }

    /// <summary>The topmost window under the point, or the monitor when over the desktop.</summary>
    public PixelRect WindowAt(Point point)
    {
        foreach (var window in Windows)
        {
            if (window.Contains(point.X, point.Y))
            {
                var clipped = window.Intersect(new PixelRect(0, 0, Image.PixelWidth, Image.PixelHeight));
                if (!clipped.IsEmpty)
                {
                    return clipped;
                }
            }
        }

        return MonitorAt(point);
    }
}
