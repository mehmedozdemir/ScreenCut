using System.Windows;

namespace ScreenCut.Core;

/// <summary>Decides where the floating toolbar goes relative to the selection.</summary>
public static class ToolbarPlacement
{
    /// <summary>
    /// Prefers below the selection (right-aligned), then above, then inside the bottom edge.
    /// The result is always clamped into <paramref name="bounds"/>.
    /// </summary>
    public static Point Compute(Rect selection, Size toolbar, Rect bounds, double margin)
    {
        var x = selection.Right - toolbar.Width;

        double y;
        if (selection.Bottom + margin + toolbar.Height <= bounds.Bottom)
        {
            y = selection.Bottom + margin;
        }
        else if (selection.Top - margin - toolbar.Height >= bounds.Top)
        {
            y = selection.Top - margin - toolbar.Height;
        }
        else
        {
            y = selection.Bottom - margin - toolbar.Height;
        }

        x = Math.Clamp(x, bounds.Left, Math.Max(bounds.Left, bounds.Right - toolbar.Width));
        y = Math.Clamp(y, bounds.Top, Math.Max(bounds.Top, bounds.Bottom - toolbar.Height));
        return new Point(x, y);
    }
}
