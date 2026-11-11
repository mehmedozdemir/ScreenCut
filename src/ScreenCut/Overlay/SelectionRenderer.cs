using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenCut.Annotations;
using ScreenCut.Core;

namespace ScreenCut.Overlay;

/// <summary>Produces the final image: the selected part of the screenshot with annotations burned in.</summary>
internal static class SelectionRenderer
{
    private const double Dpi = 96;

    public static BitmapSource Render(BitmapSource screen, PixelRect area, IEnumerable<Annotation> annotations)
    {
        if (area.IsEmpty)
        {
            throw new ArgumentException("Seçim alanı boş.", nameof(area));
        }

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.PushTransform(new TranslateTransform(-area.X, -area.Y));
            context.DrawImage(new CroppedBitmap(screen, new Int32Rect(area.X, area.Y, area.Width, area.Height)), area.ToRect());
            foreach (var annotation in annotations)
            {
                annotation.Render(context);
            }

            context.Pop();
        }

        var bitmap = new RenderTargetBitmap(area.Width, area.Height, Dpi, Dpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>Block-averages the screenshot under <paramref name="area"/> for the pixelate tool.</summary>
    public static PixelatedImage? Pixelate(BitmapSource screen, Rect area, int blockSize)
    {
        var pixels = PixelRect.FromRect(Rect.Intersect(area, new Rect(0, 0, screen.PixelWidth, screen.PixelHeight)));
        if (pixels.IsEmpty)
        {
            return null;
        }

        const int bytesPerPixel = 4;
        var stride = pixels.Width * bytesPerPixel;
        var buffer = new byte[stride * pixels.Height];
        screen.CopyPixels(new Int32Rect(pixels.X, pixels.Y, pixels.Width, pixels.Height), buffer, stride, 0);

        var averaged = Pixelation.Average(buffer, pixels.Width, pixels.Height, stride, blockSize, out var width, out var height);
        var bitmap = BitmapSource.Create(width, height, Dpi, Dpi, PixelFormats.Bgr32, null, averaged, width * bytesPerPixel);
        bitmap.Freeze();

        // The last block row/column may be partial; stretch so blocks stay aligned with the source.
        var coverage = new Rect(pixels.X, pixels.Y, width * blockSize, height * blockSize);
        var clip = pixels.ToRect();
        return new PixelatedImage(clip, Crop(bitmap, coverage, clip, blockSize));
    }

    private static BitmapSource Crop(BitmapSource blocks, Rect coverage, Rect clip, int blockSize)
    {
        if (coverage.Size == clip.Size)
        {
            return blocks;
        }

        // Render the blocks at full size, then cut to the exact area.
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.NearestNeighbor);
            using (var groupContext = group.Open())
            {
                groupContext.DrawImage(blocks, new Rect(0, 0, blocks.PixelWidth * blockSize, blocks.PixelHeight * blockSize));
            }

            context.DrawDrawing(group);
        }

        var full = new RenderTargetBitmap((int)coverage.Width, (int)coverage.Height, Dpi, Dpi, PixelFormats.Pbgra32);
        full.Render(visual);
        var cropped = new CroppedBitmap(full, new Int32Rect(0, 0, (int)clip.Width, (int)clip.Height));
        cropped.Freeze();
        return cropped;
    }
}
