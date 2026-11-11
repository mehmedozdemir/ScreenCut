using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenCut.Core;

/// <summary>Loads and saves <see cref="AppSettings"/>; corrupt or missing files fall back to defaults.</summary>
public sealed class SettingsStore(string filePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public string FilePath { get; } = filePath;

    public bool Exists => File.Exists(FilePath);

    public AppSettings Load()
    {
        if (!Exists)
        {
            return new AppSettings().Normalize();
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return (settings ?? new AppSettings()).Normalize();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            Log.Error(ex, $"Ayarlar okunamadı, varsayılanlar kullanılıyor: {FilePath}");
            return new AppSettings().Normalize();
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write to a temp file first so a crash mid-write never leaves a truncated settings file.
        var tempPath = FilePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(tempPath, FilePath, overwrite: true);
    }
}
