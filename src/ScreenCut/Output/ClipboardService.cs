using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScreenCut.Output;

/// <summary>Clipboard writes with retry (another app may briefly hold the clipboard open).</summary>
internal static class ClipboardService
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>Puts the image on the clipboard as both bitmap (DIB) and PNG for best app compatibility.</summary>
    public static void SetImage(BitmapSource image)
    {
        var opaque = new FormatConvertedBitmap(image, PixelFormats.Bgr32, null, 0);
        opaque.Freeze();

        var data = new DataObject();
        data.SetImage(opaque);

        var png = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(opaque));
        encoder.Save(png);
        data.SetData("PNG", png, autoConvert: false);

        SetWithRetry(data);
    }

    /// <summary>
    /// Puts a saved file on the clipboard twice: as a file (paste into Explorer/Teams/Outlook attaches it)
    /// and as its full path text (paste into any text field).
    /// </summary>
    public static void SetFileAndPath(string path)
    {
        var data = new DataObject();
        data.SetFileDropList([path]);
        data.SetText(path);
        SetWithRetry(data);
    }

    private static void SetWithRetry(DataObject data)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                Clipboard.SetDataObject(data, copy: true);
                return;
            }
            catch (COMException) when (attempt < MaxAttempts)
            {
                Thread.Sleep(RetryDelay);
            }
        }
    }
}
