using System.Windows;
using System.Windows.Media;

namespace ScreenCut.Annotations;

/// <summary>
/// A drawable mark on the screenshot. Coordinates are physical pixels relative to the captured image,
/// so the same <see cref="Render"/> code draws both the live overlay and the exported image.
/// </summary>
internal abstract class Annotation(Color color, double thickness)
{
    public Color Color { get; private set; } = color;

    public double Thickness { get; } = thickness;

    public abstract Rect Bounds { get; }

    /// <summary>False for accidental clicks that produced an invisible mark.</summary>
    public virtual bool IsMeaningful => true;

    public virtual bool SupportsColor => true;

    public abstract void Render(DrawingContext context);

    public abstract void Offset(Vector delta);

    /// <summary>Updates the shape while the user drags from <paramref name="start"/> to <paramref name="current"/>.</summary>
    public virtual void UpdateDrag(Point start, Point current, bool constrain)
    {
    }

    public virtual bool HitTest(Point point, double tolerance)
    {
        var bounds = Bounds;
        bounds.Inflate(tolerance, tolerance);
        return bounds.Contains(point);
    }

    public void SetColor(Color color) => Color = color;

    public virtual Annotation Clone() => (Annotation)MemberwiseClone();

    protected SolidColorBrush CreateBrush(byte? alpha = null)
    {
        var color = alpha is { } a ? Color.FromArgb(a, Color.R, Color.G, Color.B) : Color;
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    protected Pen CreatePen(double thickness, byte? alpha = null)
    {
        var pen = new Pen(CreateBrush(alpha), thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };
        pen.Freeze();
        return pen;
    }

    /// <summary>Black or white, whichever reads better on top of <paramref name="color"/>.</summary>
    protected static Color ContrastColor(Color color)
    {
        var luminance = (0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B);
        return luminance > 160 ? Colors.Black : Colors.White;
    }
}
