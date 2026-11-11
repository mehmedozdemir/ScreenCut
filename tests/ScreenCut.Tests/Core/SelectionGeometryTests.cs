using System.Windows;
using ScreenCut.Core;

namespace ScreenCut.Tests.Core;

public class SelectionGeometryTests
{
    private static readonly Rect Bounds = new(0, 0, 1000, 800);

    [Fact]
    public void FromPoints_DragUpLeft_NormalizesRect()
    {
        var rect = SelectionGeometry.FromPoints(new Point(100, 100), new Point(40, 60), constrainSquare: false);

        Assert.Equal(new Rect(40, 60, 60, 40), rect);
    }

    [Fact]
    public void FromPoints_Constrained_ProducesSquareInDragDirection()
    {
        var rect = SelectionGeometry.FromPoints(new Point(100, 100), new Point(130, 50), constrainSquare: true);

        Assert.Equal(new Rect(100, 50, 50, 50), rect);
    }

    [Fact]
    public void ConstrainAngle_NearHorizontal_SnapsToHorizontal()
    {
        var end = SelectionGeometry.ConstrainAngle(new Point(0, 0), new Point(100, 10));

        Assert.Equal(0, end.Y, precision: 6);
        Assert.Equal(Math.Sqrt((100 * 100) + (10 * 10)), end.X, precision: 6);
    }

    [Fact]
    public void Resize_RightHandle_ChangesWidthOnly()
    {
        var rect = SelectionGeometry.Resize(new Rect(10, 10, 100, 50), SelectionHandle.Right, new Vector(20, 99));

        Assert.Equal(new Rect(10, 10, 120, 50), rect);
    }

    [Fact]
    public void Resize_DraggedPastOppositeEdge_FlipsAndStaysNormalized()
    {
        var rect = SelectionGeometry.Resize(new Rect(10, 10, 100, 50), SelectionHandle.Left, new Vector(150, 0));

        Assert.Equal(new Rect(110, 10, 50, 50), rect);
    }

    [Fact]
    public void Move_PastBounds_IsClampedInside()
    {
        var rect = SelectionGeometry.Move(new Rect(900, 700, 100, 100), new Vector(500, 500), Bounds);

        Assert.Equal(new Rect(900, 700, 100, 100), rect);
    }

    [Fact]
    public void Clamp_PartiallyOutside_IsIntersected()
    {
        var rect = SelectionGeometry.Clamp(new Rect(-50, -50, 100, 100), Bounds);

        Assert.Equal(new Rect(0, 0, 50, 50), rect);
    }

    [Fact]
    public void Clamp_FullyOutside_IsEmpty()
    {
        Assert.True(SelectionGeometry.Clamp(new Rect(2000, 2000, 10, 10), Bounds).IsEmpty);
    }

    [Fact]
    public void HitTestHandle_NearCorner_ReturnsCorner()
    {
        var handle = SelectionGeometry.HitTestHandle(new Rect(10, 10, 100, 100), new Point(108, 113), tolerance: 5);

        Assert.Equal(SelectionHandle.BottomRight, handle);
    }

    [Fact]
    public void HitTestHandle_Center_ReturnsNone()
    {
        Assert.Equal(SelectionHandle.None, SelectionGeometry.HitTestHandle(new Rect(10, 10, 100, 100), new Point(60, 60), 5));
    }

    [Fact]
    public void DistanceToSegment_PerpendicularPoint_ReturnsOffset()
    {
        Assert.Equal(5, SelectionGeometry.DistanceToSegment(new Point(50, 5), new Point(0, 0), new Point(100, 0)), precision: 6);
    }

    [Fact]
    public void DistanceToSegment_BeyondEnd_MeasuresToEndpoint()
    {
        Assert.Equal(5, SelectionGeometry.DistanceToSegment(new Point(103, 4), new Point(0, 0), new Point(100, 0)), precision: 6);
    }

    [Fact]
    public void Snap_RoundsEdgesToWholePixels()
    {
        Assert.Equal(new Rect(10, 20, 31, 40), SelectionGeometry.Snap(new Rect(10.4, 19.6, 30.8, 40.2)));
    }
}
