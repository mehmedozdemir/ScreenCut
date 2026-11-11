using ScreenCut.Core;

namespace ScreenCut.Tests.Core;

public class FileNameGeneratorTests
{
    private static readonly DateTime Timestamp = new(2026, 11, 11, 14, 32, 5);

    [Fact]
    public void Generate_DefaultPattern_ReplacesAllTokens()
    {
        var name = FileNameGenerator.Generate(FileNameGenerator.DefaultPattern, Timestamp);

        Assert.Equal("ScreenCut_2026-11-11_14-32-05", name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Generate_EmptyPattern_FallsBackToDefault(string? pattern)
    {
        var name = FileNameGenerator.Generate(pattern, Timestamp);

        Assert.Equal("ScreenCut_2026-11-11_14-32-05", name);
    }

    [Fact]
    public void Generate_InvalidCharacters_AreReplaced()
    {
        var name = FileNameGenerator.Generate("a:b*c?{yyyy}", Timestamp);

        Assert.Equal("a_b_c_2026", name);
    }

    [Fact]
    public void Generate_OnlyDots_UsesFallbackName()
    {
        Assert.Equal(FileNameGenerator.FallbackName, FileNameGenerator.Generate("...", Timestamp));
    }

    [Theory]
    [InlineData("Shot_{yyyy}{MM}{dd}")]
    [InlineData("rapor")]
    public void Validate_ValidPattern_ReturnsNull(string pattern)
    {
        Assert.Null(FileNameGenerator.Validate(pattern));
    }

    [Theory]
    [InlineData("")]
    [InlineData("bad|name")]
    [InlineData("a<b>")]
    public void Validate_InvalidPattern_ReturnsMessage(string pattern)
    {
        Assert.NotNull(FileNameGenerator.Validate(pattern));
    }

    [Fact]
    public void GetUniquePath_FirstFree_ReturnsPlainName()
    {
        var path = FileNameGenerator.GetUniquePath(@"C:\shots", "img", ".png", _ => false);

        Assert.Equal(@"C:\shots\img.png", path);
    }

    [Fact]
    public void GetUniquePath_Existing_AppendsCounter()
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\shots\img.png", @"C:\shots\img_2.png" };

        var path = FileNameGenerator.GetUniquePath(@"C:\shots", "img", ".png", taken.Contains);

        Assert.Equal(@"C:\shots\img_3.png", path);
    }

    [Theory]
    [InlineData(ImageFormatKind.Png, ".png")]
    [InlineData(ImageFormatKind.Jpeg, ".jpg")]
    public void Extension_MatchesFormat(ImageFormatKind format, string expected)
    {
        Assert.Equal(expected, FileNameGenerator.Extension(format));
    }
}
