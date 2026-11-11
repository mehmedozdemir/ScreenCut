using System.Windows;
using System.Windows.Media;
using ScreenCut.Core;

namespace ScreenCut.Annotations;

/// <summary>Freehand pen or semi-transparent highlighter stroke.</summary>
internal sealed class StrokeAnnotation(Point start, Color color, double thickness, bool isHighlighter) : Annotation(color, thickness)
{
    private const double MinimumPointDistance = 1;
    private const double HighlighterWidthFactor = 3.5;
    private const byte HighlighterAlpha = 0x70;

    private List<Point> _points = [start];

    public bool IsHighlighter { get; } = isHighlighter;

    public IReadOnlyList<Point> Points => _points;

    public override Rect Bounds
    {
        get
        {
            var rect = new Rect(_points[0], _points[0]);
            foreach (var point in _points)
            {
                rect.Union(point);
            }

            rect.Inflate(EffectiveThickness / 2, EffectiveThickness / 2);
            return rect;
        }
    }

    public override bool IsMeaningful => _points.Count > 1;

    private double EffectiveThickness => IsHighlighter ? Thickness * HighlighterWidthFactor : Thickness;

    public override void UpdateDrag(Point start, Point current, bool constrain)
    {
        if (constrain)
        {
            _points = [start, SelectionGeometry.ConstrainAngle(start, current)];
            return;
        }

        if ((current - _points[^1]).Length >= MinimumPointDistance)
        {
            _points.Add(current);
        }
    }

    public override void Offset(Vector delta) => _points = _points.Select(p => p + delta).ToList();

    public override bool HitTest(Point point, double tolerance)
    {
        var limit = tolerance + (EffectiveThickness / 2);
        for (var i = 1; i < _points.Count; i++)
        {
            if (SelectionGeometry.DistanceToSegment(point, _points[i - 1], _points[i]) <= limit)
            {
                return true;
            }
        }

        return false;
    }

    public override Annotation Clone()
    {
        var copy = (StrokeAnnotation)MemberwiseClone();
        copy._points = [.. _points];
        return copy;
    }

    public override void Render(DrawingContext context)
    {
        var pen = CreatePen(EffectiveThickness, IsHighlighter ? HighlighterAlpha : null);
        if (_points.Count == 1)
        {
            context.DrawEllipse(pen.Brush, null, _points[0], EffectiveThickness / 2, EffectiveThickness / 2);
            return;
        }

        // A single geometry keeps highlighter overlaps from stacking their transparency.
        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(_points[0], isFilled: false, isClosed: false);
            stream.PolyLineTo(_points.Skip(1).ToList(), isStroked: true, isSmoothJoin: true);
        }

        geometry.Freeze();
        context.DrawGeometry(null, pen, geometry);
    }
}
