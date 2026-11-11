using ScreenCut.Core;

namespace ScreenCut.Tests.Core;

public class PixelationTests
{
    private const int BytesPerPixel = 4;

    [Fact]
    public void Average_UniformBlock_KeepsColor()
    {
        var pixels = Fill(4, 4, blue: 10, green: 20, red: 30);

        var result = Pixelation.Average(pixels, 4, 4, 4 * BytesPerPixel, blockSize: 4, out var width, out var height);

        Assert.Equal((1, 1), (width, height));
        Assert.Equal(new byte[] { 10, 20, 30, 255 }, result);
    }

    [Fact]
    public void Average_MixedBlock_ReturnsMean()
    {
        // 2×1 image: black + white → one gray block
        var pixels = new byte[] { 0, 0, 0, 255, 200, 200, 200, 255 };

        var result = Pixelation.Average(pixels, 2, 1, 2 * BytesPerPixel, blockSize: 2, out _, out _);

        Assert.Equal(new byte[] { 100, 100, 100, 255 }, result);
    }

    [Fact]
    public void Average_PartialBlocks_RoundsOutputSizeUp()
    {
        var pixels = Fill(10, 7, 0, 0, 0);

        Pixelation.Average(pixels, 10, 7, 10 * BytesPerPixel, blockSize: 4, out var width, out var height);

        Assert.Equal((3, 2), (width, height));
    }

    [Fact]
    public void Average_BufferTooSmall_Throws()
    {
        Assert.Throws<ArgumentException>(() => Pixelation.Average(new byte[8], 4, 4, 16, 2, out _, out _));
    }

    [Fact]
    public void Average_ZeroBlockSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Pixelation.Average(Fill(2, 2, 0, 0, 0), 2, 2, 8, 0, out _, out _));
    }

    private static byte[] Fill(int width, int height, byte blue, byte green, byte red)
    {
        var buffer = new byte[width * height * BytesPerPixel];
        for (var i = 0; i < buffer.Length; i += BytesPerPixel)
        {
            buffer[i] = blue;
            buffer[i + 1] = green;
            buffer[i + 2] = red;
            buffer[i + 3] = 255;
        }

        return buffer;
    }
}
