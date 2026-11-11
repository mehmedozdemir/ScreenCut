using System.Reflection;

namespace ScreenCut.Core;

/// <summary>Application identity, version and well-known paths.</summary>
internal static class AppInfo
{
    public const string Name = "ScreenCut";
    public const string RepositoryUrl = "https://github.com/mehmedozdemir/ScreenCut";
    public const string ReleasesUrl = RepositoryUrl + "/releases";

    public static string Version { get; } = ReadVersion();

    public static string SettingsFile { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Name, "settings.json");

    public static string LogFile { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Name, "logs", "screencut.log");

    public static string TempFolder { get; } = Path.Combine(Path.GetTempPath(), Name);

    private static string ReadVersion()
    {
        var informational = typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrEmpty(informational))
        {
            return "0.0.0";
        }

        // Strip the "+<commit sha>" build metadata for display.
        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        return plus > 0 ? informational[..plus] : informational;
    }
}
