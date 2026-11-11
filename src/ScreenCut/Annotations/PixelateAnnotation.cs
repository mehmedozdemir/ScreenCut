using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenCut.Core;

namespace ScreenCut.Annotations;

/// <summary>A pixelated image patch covering <see cref="PixelatedImage.Area"/>.</summary>
internal sealed record PixelatedImage(Rect Area, BitmapSource Bitmap);

/// <summary>Hides sensitive content (passwords, personal data) by pixelating a rectangle.</summary>
internal sealed class PixelateAnnotation(Point start, Func<Rect, PixelatedImage?> pixelator) : Annotation(Colors.Gray, 1)
{
    private const double MinimumSize = 4;
    private PixelatedImage? _image;

    public Rect Area { get; private set; } = new(start, start);

    public override Rect Bounds => Area;

    public override bool IsMeaningful => Area.Width >= MinimumSize && Area.Height >= MinimumSize;

    public override bool SupportsColor => false;

    public override void UpdateDrag(Point start, Point current, bool constrain)
    {
        Area = SelectionGeometry.FromPoints(start, current, constrain);
        _image = null;
    }

    public override void Offset(Vector delta)
    {
        var area = Area;
        area.Offset(delta);
        Area = area;
        _image = null;
    }

    /// <summary>Samples the screenshot under <see cref="Area"/>. Call after drawing or moving finishes.</summary>
    public void Complete() => _image = pixelator(Area);

    public override void Render(DrawingContext context)
    {
        if (_image is null)
        {
            var fill = new SolidColorBrush(Color.FromArgb(0x99, 0x80, 0x80, 0x80));
            fill.Freeze();
            var outline = new Pen(Brushes.White, 1) { DashStyle = DashStyles.Dash };
            outline.Freeze();
            context.DrawRectangle(fill, outline, Area);
            return;
        }

        var group = new DrawingGroup();
        RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.NearestNeighbor);
        using (var groupContext = group.Open())
        {
            groupContext.DrawImage(_image.Bitmap, _image.Area);
        }

        context.DrawDrawing(group);
    }
}
