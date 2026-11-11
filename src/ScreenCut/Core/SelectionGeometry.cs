using System.Windows;

namespace ScreenCut.Core;

public enum SelectionHandle
{
    None,
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left,
}

/// <summary>Pure geometry helpers for the selection rectangle and drawing tools.</summary>
public static class SelectionGeometry
{
    private const double SnapAngle = Math.PI / 4;

    public static Rect FromPoints(Point start, Point end, bool constrainSquare)
    {
        var corner = constrainSquare ? ConstrainSquare(start, end) : end;
        return new Rect(start, corner);
    }

    /// <summary>Moves <paramref name="end"/> so that start→end forms a square.</summary>
    public static Point ConstrainSquare(Point start, Point end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var side = Math.Max(Math.Abs(dx), Math.Abs(dy));
        return new Point(start.X + (Math.Sign(dx) == 0 ? 1 : Math.Sign(dx)) * side, start.Y + (Math.Sign(dy) == 0 ? 1 : Math.Sign(dy)) * side);
    }

    /// <summary>Snaps the direction start→end to the nearest multiple of 45°.</summary>
    public static Point ConstrainAngle(Point start, Point end)
    {
        var vector = end - start;
        var length = vector.Length;
        if (length < double.Epsilon)
        {
            return end;
        }

        var angle = Math.Round(Math.Atan2(vector.Y, vector.X) / SnapAngle) * SnapAngle;
        return new Point(start.X + Math.Cos(angle) * length, start.Y + Math.Sin(angle) * length);
    }

    public static Rect Resize(Rect original, SelectionHandle handle, Vector delta)
    {
        double left = original.Left, top = original.Top, right = original.Right, bottom = original.Bottom;
        switch (handle)
        {
            case SelectionHandle.TopLeft: left += delta.X; top += delta.Y; break;
            case SelectionHandle.Top: top += delta.Y; break;
            case SelectionHandle.TopRight: right += delta.X; top += delta.Y; break;
            case SelectionHandle.Right: right += delta.X; break;
            case SelectionHandle.BottomRight: right += delta.X; bottom += delta.Y; break;
            case SelectionHandle.Bottom: bottom += delta.Y; break;
            case SelectionHandle.BottomLeft: left += delta.X; bottom += delta.Y; break;
            case SelectionHandle.Left: left += delta.X; break;
            case SelectionHandle.None:
            default:
                return original;
        }

        return new Rect(new Point(Math.Min(left, right), Math.Min(top, bottom)), new Point(Math.Max(left, right), Math.Max(top, bottom)));
    }

    /// <summary>Offsets the rectangle while keeping it fully inside <paramref name="bounds"/>.</summary>
    public static Rect Move(Rect original, Vector delta, Rect bounds)
    {
        var x = Math.Clamp(original.X + delta.X, bounds.Left, Math.Max(bounds.Left, bounds.Right - original.Width));
        var y = Math.Clamp(original.Y + delta.Y, bounds.Top, Math.Max(bounds.Top, bounds.Bottom - original.Height));
        return new Rect(x, y, original.Width, original.Height);
    }

    public static Rect Clamp(Rect rect, Rect bounds)
    {
        if (rect.IsEmpty)
        {
            return Rect.Empty;
        }

        var result = Rect.Intersect(rect, bounds);
        return result.IsEmpty ? Rect.Empty : result;
    }

    public static Point Clamp(Point point, Rect bounds) =>
        new(Math.Clamp(point.X, bounds.Left, bounds.Right), Math.Clamp(point.Y, bounds.Top, bounds.Bottom));

    /// <summary>Rounds the rectangle edges to whole pixels.</summary>
    public static Rect Snap(Rect rect) => rect.IsEmpty ? Rect.Empty : PixelRect.FromRect(rect).ToRect();

    public static IReadOnlyList<(SelectionHandle Handle, Point Position)> GetHandlePositions(Rect rect)
    {
        var midX = rect.Left + rect.Width / 2;
        var midY = rect.Top + rect.Height / 2;
        return
        [
            (SelectionHandle.TopLeft, rect.TopLeft),
            (SelectionHandle.Top, new Point(midX, rect.Top)),
            (SelectionHandle.TopRight, rect.TopRight),
            (SelectionHandle.Right, new Point(rect.Right, midY)),
            (SelectionHandle.BottomRight, rect.BottomRight),
            (SelectionHandle.Bottom, new Point(midX, rect.Bottom)),
            (SelectionHandle.BottomLeft, rect.BottomLeft),
            (SelectionHandle.Left, new Point(rect.Left, midY)),
        ];
    }

    public static SelectionHandle HitTestHandle(Rect rect, Point point, double tolerance)
    {
        if (rect.IsEmpty)
        {
            return SelectionHandle.None;
        }

        foreach (var (handle, position) in GetHandlePositions(rect))
        {
            if (Math.Abs(point.X - position.X) <= tolerance && Math.Abs(point.Y - position.Y) <= tolerance)
            {
                return handle;
            }
        }

        return SelectionHandle.None;
    }

    public static double DistanceToSegment(Point point, Point a, Point b)
    {
        var segment = b - a;
        var lengthSquared = segment.LengthSquared;
        if (lengthSquared < double.Epsilon)
        {
            return (point - a).Length;
        }

        var t = Math.Clamp(Vector.Multiply(point - a, segment) / lengthSquared, 0, 1);
        var projection = a + t * segment;
        return (point - projection).Length;
    }
}
