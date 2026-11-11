namespace ScreenCut.Core;

public enum ImageFormatKind
{
    Png,
    Jpeg,
}

/// <summary>User preferences persisted as JSON in %AppData%\ScreenCut\settings.json.</summary>
public sealed class AppSettings
{
    public const int MinJpegQuality = 50;
    public const int MaxJpegQuality = 100;
    public const int MaxSizeLevel = 2;
    public const string DefaultColor = "#E53935";

    public string SaveFolder { get; set; } = DefaultSaveFolder;

    public string FileNamePattern { get; set; } = FileNameGenerator.DefaultPattern;

    public ImageFormatKind ImageFormat { get; set; } = ImageFormatKind.Png;

    public int JpegQuality { get; set; } = 90;

    public bool StartWithWindows { get; set; } = true;

    public string LastTool { get; set; } = "Rectangle";

    public string LastColor { get; set; } = DefaultColor;

    public int LastSizeLevel { get; set; } = 1;

    public bool TextBackground { get; set; } = true;

    public static string DefaultSaveFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "ScreenCut");

    /// <summary>Replaces out-of-range or malformed values with safe defaults.</summary>
    public AppSettings Normalize()
    {
        if (string.IsNullOrWhiteSpace(SaveFolder) || !Path.IsPathFullyQualified(SaveFolder))
        {
            SaveFolder = DefaultSaveFolder;
        }

        if (FileNameGenerator.Validate(FileNamePattern) is not null)
        {
            FileNamePattern = FileNameGenerator.DefaultPattern;
        }

        if (!Enum.IsDefined(ImageFormat))
        {
            ImageFormat = ImageFormatKind.Png;
        }

        JpegQuality = Math.Clamp(JpegQuality, MinJpegQuality, MaxJpegQuality);
        LastSizeLevel = Math.Clamp(LastSizeLevel, 0, MaxSizeLevel);
        LastTool = string.IsNullOrWhiteSpace(LastTool) ? "Rectangle" : LastTool;
        LastColor = string.IsNullOrWhiteSpace(LastColor) ? DefaultColor : LastColor;
        return this;
    }

    public AppSettings Clone() => (AppSettings)MemberwiseClone();
}
