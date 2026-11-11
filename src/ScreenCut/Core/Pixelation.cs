namespace ScreenCut.Core;

/// <summary>Block-average pixelation used by the "pixelate" (blur) tool to hide sensitive content.</summary>
public static class Pixelation
{
    private const int BytesPerPixel = 4;

    /// <summary>
    /// Averages <paramref name="blockSize"/>×<paramref name="blockSize"/> blocks of a 32-bit BGRA/BGR32 buffer.
    /// Returns an opaque BGR32 buffer of size <paramref name="outWidth"/>×<paramref name="outHeight"/>.
    /// </summary>
    public static byte[] Average(byte[] pixels, int width, int height, int stride, int blockSize, out int outWidth, out int outHeight)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(blockSize);
        if (stride < width * BytesPerPixel || pixels.Length < stride * height)
        {
            throw new ArgumentException("Piksel arabelleği boyutlarla uyuşmuyor.", nameof(pixels));
        }

        outWidth = (width + blockSize - 1) / blockSize;
        outHeight = (height + blockSize - 1) / blockSize;
        var result = new byte[outWidth * outHeight * BytesPerPixel];

        for (var blockY = 0; blockY < outHeight; blockY++)
        {
            for (var blockX = 0; blockX < outWidth; blockX++)
            {
                var target = (blockY * outWidth + blockX) * BytesPerPixel;
                AverageBlock(pixels, stride, blockX * blockSize, blockY * blockSize, Math.Min(blockSize, width - blockX * blockSize), Math.Min(blockSize, height - blockY * blockSize), result, target);
            }
        }

        return result;
    }

    private static void AverageBlock(byte[] pixels, int stride, int startX, int startY, int blockWidth, int blockHeight, byte[] result, int target)
    {
        long blue = 0, green = 0, red = 0;
        for (var y = startY; y < startY + blockHeight; y++)
        {
            var row = y * stride;
            for (var x = startX; x < startX + blockWidth; x++)
            {
                var index = row + x * BytesPerPixel;
                blue += pixels[index];
                green += pixels[index + 1];
                red += pixels[index + 2];
            }
        }

        var count = blockWidth * blockHeight;
        result[target] = (byte)(blue / count);
        result[target + 1] = (byte)(green / count);
        result[target + 2] = (byte)(red / count);
        result[target + 3] = 255;
    }
}
