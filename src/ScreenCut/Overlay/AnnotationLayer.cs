using System.Windows;
using System.Windows.Media;
using ScreenCut.Annotations;

namespace ScreenCut.Overlay;

/// <summary>Lightweight visual that draws all annotations, the in-progress draft and the selection marker.</summary>
internal sealed class AnnotationLayer : FrameworkElement
{
    private static readonly Color SelectionMarkerColor = Color.FromRgb(0x2D, 0x7F, 0xF9);

    public AnnotationLayer() => IsHitTestVisible = false;

    public AnnotationDocument? Document { get; set; }

    public Annotation? Draft { get; set; }

    public Annotation? Selected { get; set; }

    /// <summary>Device pixels per DIP, so chrome lines keep a constant on-screen width.</summary>
    public double ChromeScale { get; set; } = 1;

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (Document is not null)
        {
            foreach (var annotation in Document.Items)
            {
                annotation.Render(drawingContext);
            }
        }

        Draft?.Render(drawingContext);

        if (Selected is not null)
        {
            var bounds = Selected.Bounds;
            bounds.Inflate(3 * ChromeScale, 3 * ChromeScale);
            var pen = new Pen(new SolidColorBrush(SelectionMarkerColor), 1.5 * ChromeScale) { DashStyle = DashStyles.Dash };
            pen.Freeze();
            drawingContext.DrawRectangle(null, pen, bounds);
        }
    }
}
