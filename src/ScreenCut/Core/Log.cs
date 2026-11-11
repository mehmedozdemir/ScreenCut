using System.Globalization;

namespace ScreenCut.Core;

/// <summary>Minimal thread-safe file logger with size-based rotation.</summary>
public static class Log
{
    private const long MaxFileSizeBytes = 1024 * 1024;
    private static readonly Lock Gate = new();
    private static string? _filePath;

    public static void Initialize(string filePath)
    {
        _filePath = filePath;
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Error(Exception exception, string message) => Write("ERROR", $"{message}{Environment.NewLine}{exception}");

    private static void Write(string level, string message)
    {
        if (_filePath is null)
        {
            return;
        }

        var line = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)} [{level}] {message}{Environment.NewLine}";
        lock (Gate)
        {
            try
            {
                RotateIfNeeded(_filePath);
                File.AppendAllText(_filePath, line);
            }
            catch (IOException)
            {
                // Logging must never crash the app; there is nowhere else to report this.
            }
        }
    }

    private static void RotateIfNeeded(string path)
    {
        var info = new FileInfo(path);
        if (info.Exists && info.Length > MaxFileSizeBytes)
        {
            File.Move(path, path + ".1", overwrite: true);
        }
    }
}
