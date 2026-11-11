using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace ScreenCut.Annotations;

/// <summary>A text note, optionally on a light rounded "label" background for readability.</summary>
internal sealed class TextAnnotation(Point position, string text, Color color, double fontSize, bool hasBackground)
    : Annotation(color, fontSize / 10)
{
    public const double PaddingFactor = 0.3;
    private const double HaloFactor = 0.18;
    private static readonly Typeface Typeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Color LabelBackground = Color.FromArgb(0xF2, 0xFF, 0xFF, 0xFF);

    public Point Position { get; private set; } = position;

    public string Text { get; } = text;

    public double FontSize { get; } = fontSize;

    public bool HasBackground { get; } = hasBackground;

    public static FontFamily FontFamily => Typeface.FontFamily;

    public static FontWeight FontWeight => Typeface.Weight;

    public double Padding => HasBackground ? FontSize * PaddingFactor : 0;

    public override Rect Bounds
    {
        get
        {
            var formatted = Format(Brushes.Black);
            return new Rect(Position, new Size(formatted.WidthIncludingTrailingWhitespace + (2 * Padding), formatted.Height + (2 * Padding)));
        }
    }

    public override bool IsMeaningful => !string.IsNullOrWhiteSpace(Text);

    public override void Offset(Vector delta) => Position += delta;

    public override void Render(DrawingContext context)
    {
        var brush = CreateBrush();
        var formatted = Format(brush);
        var origin = new Point(Position.X + Padding, Position.Y + Padding);

        if (HasBackground)
        {
            var background = new SolidColorBrush(LabelBackground);
            background.Freeze();
            var radius = FontSize * 0.25;
            context.DrawRoundedRectangle(background, CreatePen(Math.Max(1, FontSize / 12)), Bounds, radius, radius);
            context.DrawText(formatted, origin);
            return;
        }

        // Without a label, a contrasting halo keeps text readable on any screenshot.
        var geometry = formatted.BuildGeometry(origin);
        var haloBrush = new SolidColorBrush(ContrastColor(Color));
        haloBrush.Freeze();
        var halo = new Pen(haloBrush, FontSize * HaloFactor) { LineJoin = PenLineJoin.Round };
        halo.Freeze();
        context.DrawGeometry(null, halo, geometry);
        context.DrawGeometry(brush, null, geometry);
    }

    private FormattedText Format(Brush brush) =>
        new(Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface, FontSize, brush, pixelsPerDip: 1.0);
}
