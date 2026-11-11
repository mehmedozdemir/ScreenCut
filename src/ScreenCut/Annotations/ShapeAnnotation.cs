using System.Windows;
using System.Windows.Media;
using ScreenCut.Core;

namespace ScreenCut.Annotations;

internal enum ShapeKind
{
    Rectangle,
    Ellipse,
    Line,
    Arrow,
}

/// <summary>Rectangle, ellipse, straight line or arrow defined by two points.</summary>
internal sealed class ShapeAnnotation(ShapeKind kind, Point start, Color color, double thickness) : Annotation(color, thickness)
{
    private const double MinimumLength = 3;
    private const double ArrowHeadFactor = 4;
    private const double MinimumArrowHead = 12;

    public ShapeKind Kind { get; } = kind;

    public Point Start { get; private set; } = start;

    public Point End { get; private set; } = start;

    public override Rect Bounds
    {
        get
        {
            var rect = new Rect(Start, End);
            var padding = Kind == ShapeKind.Arrow ? ArrowHeadLength : Thickness;
            rect.Inflate(padding, padding);
            return rect;
        }
    }

    public override bool IsMeaningful => (End - Start).Length >= MinimumLength;

    private double ArrowHeadLength => Math.Max(Thickness * ArrowHeadFactor, MinimumArrowHead);

    public override void UpdateDrag(Point start, Point current, bool constrain)
    {
        Start = start;
        End = !constrain
            ? current
            : Kind is ShapeKind.Line or ShapeKind.Arrow
                ? SelectionGeometry.ConstrainAngle(start, current)
                : SelectionGeometry.ConstrainSquare(start, current);
    }

    public override void Offset(Vector delta)
    {
        Start += delta;
        End += delta;
    }

    public override bool HitTest(Point point, double tolerance) => Kind switch
    {
        ShapeKind.Line or ShapeKind.Arrow => SelectionGeometry.DistanceToSegment(point, Start, End) <= tolerance + Thickness,
        _ => base.HitTest(point, tolerance),
    };

    public override void Render(DrawingContext context)
    {
        var pen = CreatePen(Thickness);
        switch (Kind)
        {
            case ShapeKind.Rectangle:
                context.DrawRectangle(null, pen, new Rect(Start, End));
                break;
            case ShapeKind.Ellipse:
                var rect = new Rect(Start, End);
                context.DrawEllipse(null, pen, new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2), rect.Width / 2, rect.Height / 2);
                break;
            case ShapeKind.Line:
                context.DrawLine(pen, Start, End);
                break;
            case ShapeKind.Arrow:
                RenderArrow(context, pen);
                break;
            default:
                throw new InvalidOperationException($"Bilinmeyen şekil: {Kind}");
        }
    }

    private void RenderArrow(DrawingContext context, Pen pen)
    {
        var direction = End - Start;
        var length = direction.Length;
        if (length < double.Epsilon)
        {
            return;
        }

        direction.Normalize();
        var head = Math.Min(ArrowHeadLength, length);
        var headBase = End - (direction * head);
        var normal = new Vector(-direction.Y, direction.X) * head * 0.55;

        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(End, isFilled: true, isClosed: true);
            stream.LineTo(headBase + normal, isStroked: true, isSmoothJoin: true);
            stream.LineTo(headBase - normal, isStroked: true, isSmoothJoin: true);
        }

        geometry.Freeze();

        // Stop the shaft inside the head so the round cap does not poke through the tip.
        context.DrawLine(pen, Start, End - (direction * head * 0.6));
        context.DrawGeometry(pen.Brush, CreatePen(1), geometry);
    }
}
