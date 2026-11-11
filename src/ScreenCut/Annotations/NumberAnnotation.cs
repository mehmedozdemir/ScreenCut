using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace ScreenCut.Annotations;

/// <summary>Numbered step marker (①②③…) for walkthrough screenshots.</summary>
internal sealed class NumberAnnotation(Point center, int number, Color color, double radius) : Annotation(color, radius / 8)
{
    private static readonly Typeface Typeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

    public Point Center { get; private set; } = center;

    public int Number { get; } = number;

    public double Radius { get; } = radius;

    public override Rect Bounds => new(Center.X - Radius, Center.Y - Radius, Radius * 2, Radius * 2);

    public override void Offset(Vector delta) => Center += delta;

    public override void Render(DrawingContext context)
    {
        var borderBrush = new SolidColorBrush(ContrastColor(Color));
        borderBrush.Freeze();
        var border = new Pen(borderBrush, Math.Max(1.5, Radius / 8));
        border.Freeze();
        context.DrawEllipse(CreateBrush(), border, Center, Radius, Radius);

        var text = new FormattedText(
            Number.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface, Radius * 1.15, borderBrush, pixelsPerDip: 1.0);
        context.DrawText(text, new Point(Center.X - (text.Width / 2), Center.Y - (text.Height / 2)));
    }
}
