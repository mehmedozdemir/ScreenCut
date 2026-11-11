using ScreenCut.Core;

namespace ScreenCut.Tests.Core;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "ScreenCut.Tests", Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_folder, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var settings = new SettingsStore(SettingsPath).Load();

        Assert.Equal(FileNameGenerator.DefaultPattern, settings.FileNamePattern);
        Assert.Equal(ImageFormatKind.Png, settings.ImageFormat);
        Assert.True(settings.StartWithWindows);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsValues()
    {
        var store = new SettingsStore(SettingsPath);
        var original = new AppSettings
        {
            SaveFolder = @"D:\Shots",
            FileNamePattern = "shot_{HH}{mm}",
            ImageFormat = ImageFormatKind.Jpeg,
            JpegQuality = 75,
            StartWithWindows = false,
            LastTool = "Arrow",
            LastColor = "#1E88E5",
            LastSizeLevel = 2,
            TextBackground = false,
        };

        store.Save(original);
        var loaded = store.Load();

        Assert.Equal(original.SaveFolder, loaded.SaveFolder);
        Assert.Equal(original.FileNamePattern, loaded.FileNamePattern);
        Assert.Equal(ImageFormatKind.Jpeg, loaded.ImageFormat);
        Assert.Equal(75, loaded.JpegQuality);
        Assert.False(loaded.StartWithWindows);
        Assert.Equal("Arrow", loaded.LastTool);
        Assert.Equal("#1E88E5", loaded.LastColor);
        Assert.Equal(2, loaded.LastSizeLevel);
        Assert.False(loaded.TextBackground);
    }

    [Fact]
    public void Load_CorruptJson_ReturnsDefaults()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(SettingsPath, "{ this is not json");

        var settings = new SettingsStore(SettingsPath).Load();

        Assert.Equal(FileNameGenerator.DefaultPattern, settings.FileNamePattern);
    }

    [Fact]
    public void Normalize_OutOfRangeValues_AreCorrected()
    {
        var settings = new AppSettings
        {
            SaveFolder = "relative\\path",
            FileNamePattern = "bad|name",
            JpegQuality = 5,
            LastSizeLevel = 9,
        }.Normalize();

        Assert.Equal(AppSettings.DefaultSaveFolder, settings.SaveFolder);
        Assert.Equal(FileNameGenerator.DefaultPattern, settings.FileNamePattern);
        Assert.Equal(AppSettings.MinJpegQuality, settings.JpegQuality);
        Assert.Equal(AppSettings.MaxSizeLevel, settings.LastSizeLevel);
    }
}
