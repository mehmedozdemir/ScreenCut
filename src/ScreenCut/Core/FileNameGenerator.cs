using System.Globalization;

namespace ScreenCut.Core;

/// <summary>Builds file names from a pattern such as <c>ScreenCut_{yyyy}-{MM}-{dd}_{HH}-{mm}-{ss}</c>.</summary>
public static class FileNameGenerator
{
    public const string DefaultPattern = "ScreenCut_{yyyy}-{MM}-{dd}_{HH}-{mm}-{ss}";
    public const string FallbackName = "ScreenCut";
    private const int MaxUniqueAttempts = 10_000;

    /// <summary>Supported placeholders; each maps to the same .NET custom date format.</summary>
    public static readonly IReadOnlyList<string> Tokens = ["yyyy", "MM", "dd", "HH", "mm", "ss"];

    public static string Generate(string? pattern, DateTime timestamp)
    {
        var name = string.IsNullOrWhiteSpace(pattern) ? DefaultPattern : pattern;
        foreach (var token in Tokens)
        {
            name = name.Replace("{" + token + "}", timestamp.ToString(token, CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        name = Sanitize(name).Trim().TrimEnd('.');
        return name.Length == 0 ? FallbackName : name;
    }

    /// <summary>Returns null when valid, otherwise a user-facing (Turkish) error message.</summary>
    public static string? Validate(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return "Dosya adı şablonu boş olamaz.";
        }

        var withoutTokens = Tokens.Aggregate(pattern, (current, token) => current.Replace("{" + token + "}", string.Empty, StringComparison.Ordinal));
        var invalid = withoutTokens.Where(c => Path.GetInvalidFileNameChars().Contains(c)).Distinct().ToArray();
        return invalid.Length == 0 ? null : $"Geçersiz karakter(ler): {string.Join(" ", invalid)}";
    }

    public static string Extension(ImageFormatKind format) => format == ImageFormatKind.Jpeg ? ".jpg" : ".png";

    /// <summary>Appends _2, _3, … until <paramref name="exists"/> returns false.</summary>
    public static string GetUniquePath(string folder, string baseName, string extension, Func<string, bool> exists)
    {
        ArgumentNullException.ThrowIfNull(exists);
        var candidate = Path.Combine(folder, baseName + extension);
        for (var i = 2; exists(candidate); i++)
        {
            if (i > MaxUniqueAttempts)
            {
                throw new IOException("Benzersiz dosya adı üretilemedi.");
            }

            candidate = Path.Combine(folder, $"{baseName}_{i}{extension}");
        }

        return candidate;
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
