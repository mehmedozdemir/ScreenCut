using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenCut.Annotations;
using ScreenCut.Capture;
using ScreenCut.Core;
using ScreenCut.Output;
using ScreenCut.Overlay;

namespace ScreenCut.Tests.Overlay;

public class OverlayRenderingTests
{
    [Fact]
    public void Render_CropsSelectionAndBurnsInAnnotations() => RunOnSta(() =>
    {
        var screen = CreateSolidBitmap(200, 100, Colors.White);
        var rectangle = new ShapeAnnotation(ShapeKind.Rectangle, new Point(60, 30), Colors.Red, 4);
        rectangle.UpdateDrag(new Point(60, 30), new Point(90, 60), constrain: false);

        var result = SelectionRenderer.Render(screen, new PixelRect(50, 20, 100, 60), [rectangle]);

        Assert.Equal(100, result.PixelWidth);
        Assert.Equal(60, result.PixelHeight);
        Assert.Equal(Colors.Red, ReadPixel(result, 10, 25)); // left edge of the rectangle (x=60 → 10)
        Assert.Equal(Colors.White, ReadPixel(result, 90, 50));
    });

    [Fact]
    public void Pixelate_ReturnsPatchCoveringArea() => RunOnSta(() =>
    {
        var screen = CreateSolidBitmap(64, 64, Colors.Blue);

        var patch = SelectionRenderer.Pixelate(screen, new Rect(5, 5, 33, 21), blockSize: 8);

        Assert.NotNull(patch);
        Assert.Equal(new Rect(5, 5, 33, 21), patch.Area);
        Assert.Equal(33, patch.Bitmap.PixelWidth);
        Assert.Equal(21, patch.Bitmap.PixelHeight);
    });

    [Fact]
    public void OverlayWindow_ConstructsWithoutXamlErrors() => RunOnSta(() =>
    {
        var snapshot = new CaptureSnapshot(
            CreateSolidBitmap(320, 200, Colors.Gray),
            new PixelRect(0, 0, 320, 200),
            [new PixelRect(10, 10, 100, 80)],
            [new PixelRect(0, 0, 320, 200)],
            null,
            new Point(50, 50));
        var actions = new OutputActions(new AppSettings(), new NullNotifier());

        var window = new OverlayWindow(snapshot, CaptureMode.Region, actions, new AppSettings { LastTool = "Arrow" });

        Assert.NotNull(window);
        window.Close();
    });

    private static BitmapSource CreateSolidBitmap(int width, int height, Color color)
    {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = color.B;
            pixels[i + 1] = color.G;
            pixels[i + 2] = color.R;
            pixels[i + 3] = 255;
        }

        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixels, width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private static Color ReadPixel(BitmapSource bitmap, int x, int y)
    {
        var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        var pixel = new byte[4];
        converted.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
        return Color.FromRgb(pixel[2], pixel[1], pixel[0]);
    }

    private static void RunOnSta(Action action)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }

    private sealed class NullNotifier : INotifier
    {
        public void Info(string title, string message, Action? onClick = null)
        {
        }

        public void Error(string title, string message)
        {
        }
    }
}
