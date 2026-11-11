using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenCut.Core;

namespace ScreenCut.Output;

/// <summary>Encodes screenshots to PNG/JPEG files.</summary>
internal static class ImageFileWriter
{
    private static readonly TimeSpan TempFileLifetime = TimeSpan.FromDays(1);

    /// <summary>Saves into the configured folder with a generated, unique name. Returns the full path.</summary>
    public static string SaveToConfiguredFolder(BitmapSource image, AppSettings settings)
    {
        var baseName = FileNameGenerator.Generate(settings.FileNamePattern, DateTime.Now);
        var extension = FileNameGenerator.Extension(settings.ImageFormat);
        Directory.CreateDirectory(settings.SaveFolder);
        var path = FileNameGenerator.GetUniquePath(settings.SaveFolder, baseName, extension, File.Exists);
        Save(image, path, settings.ImageFormat, settings.JpegQuality);
        return path;
    }

    /// <summary>Saves a PNG into %TEMP%\ScreenCut for sharing / e-mail attachments.</summary>
    public static string SaveTemporary(BitmapSource image)
    {
        Directory.CreateDirectory(AppInfo.TempFolder);
        var baseName = FileNameGenerator.Generate(FileNameGenerator.DefaultPattern, DateTime.Now);
        var path = FileNameGenerator.GetUniquePath(AppInfo.TempFolder, baseName, ".png", File.Exists);
        Save(image, path, ImageFormatKind.Png, 100);
        return path;
    }

    public static void Save(BitmapSource image, string path, ImageFormatKind format, int jpegQuality)
    {
        BitmapEncoder encoder = format == ImageFormatKind.Jpeg
            ? new JpegBitmapEncoder { QualityLevel = Math.Clamp(jpegQuality, AppSettings.MinJpegQuality, AppSettings.MaxJpegQuality) }
            : new PngBitmapEncoder();
        var pixelFormat = format == ImageFormatKind.Jpeg ? PixelFormats.Bgr24 : PixelFormats.Bgr32;
        encoder.Frames.Add(BitmapFrame.Create(new FormatConvertedBitmap(image, pixelFormat, null, 0)));

        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        encoder.Save(stream);
    }

    /// <summary>Removes temp share files older than a day.</summary>
    public static void CleanupTemporaryFiles()
    {
        if (!Directory.Exists(AppInfo.TempFolder))
        {
            return;
        }

        foreach (var file in new DirectoryInfo(AppInfo.TempFolder).EnumerateFiles())
        {
            if (DateTime.UtcNow - file.LastWriteTimeUtc <= TempFileLifetime)
            {
                continue;
            }

            try
            {
                file.Delete();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Error(ex, $"Geçici dosya silinemedi: {file.FullName}");
            }
        }
    }
}
