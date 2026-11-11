using System.Windows;

namespace ScreenCut.Core;

/// <summary>Integer rectangle in physical screen pixels.</summary>
public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;

    public int Bottom => Y + Height;

    public bool IsEmpty => Width <= 0 || Height <= 0;

    public bool Contains(double x, double y) => x >= X && y >= Y && x < Right && y < Bottom;

    public PixelRect Offset(int dx, int dy) => this with { X = X + dx, Y = Y + dy };

    public PixelRect Intersect(PixelRect other)
    {
        var left = Math.Max(X, other.X);
        var top = Math.Max(Y, other.Y);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);
        return right <= left || bottom <= top ? default : new PixelRect(left, top, right - left, bottom - top);
    }

    public Rect ToRect() => new(X, Y, Width, Height);

    /// <summary>Rounds each edge to the nearest pixel.</summary>
    public static PixelRect FromRect(Rect rect)
    {
        if (rect.IsEmpty)
        {
            return default;
        }

        var left = (int)Math.Round(rect.Left);
        var top = (int)Math.Round(rect.Top);
        var right = (int)Math.Round(rect.Right);
        var bottom = (int)Math.Round(rect.Bottom);
        return new PixelRect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }
}
