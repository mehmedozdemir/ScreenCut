using System.Windows;
using ScreenCut.Core;

namespace ScreenCut.Tests.Core;

public class ToolbarPlacementTests
{
    private static readonly Rect Monitor = new(0, 0, 1920, 1080);
    private static readonly Size Toolbar = new(400, 80);
    private const double Margin = 8;

    [Fact]
    public void Compute_RoomBelow_PlacesBelowRightAligned()
    {
        var position = ToolbarPlacement.Compute(new Rect(500, 200, 600, 300), Toolbar, Monitor, Margin);

        Assert.Equal(new Point(700, 508), position);
    }

    [Fact]
    public void Compute_NoRoomBelow_PlacesAbove()
    {
        var position = ToolbarPlacement.Compute(new Rect(500, 600, 600, 450), Toolbar, Monitor, Margin);

        Assert.Equal(new Point(700, 512), position);
    }

    [Fact]
    public void Compute_FullScreenSelection_PlacesInsideBottom()
    {
        var position = ToolbarPlacement.Compute(Monitor, Toolbar, Monitor, Margin);

        Assert.Equal(new Point(1520, 992), position);
    }

    [Fact]
    public void Compute_SelectionNearLeftEdge_IsClampedToMonitor()
    {
        var position = ToolbarPlacement.Compute(new Rect(10, 100, 50, 50), Toolbar, Monitor, Margin);

        Assert.Equal(0, position.X);
    }

    [Fact]
    public void Compute_SecondaryMonitorWithOffset_StaysOnThatMonitor()
    {
        var secondary = new Rect(1920, 0, 1280, 1024);

        var position = ToolbarPlacement.Compute(new Rect(1930, 900, 100, 100), Toolbar, secondary, Margin);

        Assert.True(position.X >= secondary.Left);
        Assert.True(position.Y + Toolbar.Height <= secondary.Bottom);
    }
}
