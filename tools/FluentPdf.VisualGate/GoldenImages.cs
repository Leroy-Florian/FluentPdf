using FluentPdf.Visual;
using SkiaSharp;

namespace FluentPdf.VisualGate;

/// <summary>
/// Reads and writes golden PNG baselines as <see cref="RasterPage"/>s (BGRA). Encoding goes
/// through the visual engine's PNG writer; decoding uses SkiaSharp.
/// </summary>
internal static class GoldenImages
{
    // Goldens are stored flattened over white (fully opaque), so they survive the PNG
    // round-trip exactly — semi-transparent pixels would otherwise be mangled on decode.
    public static byte[] Encode(RasterPage page) =>
        PngImage.EncodeRgba(page.Width, page.Height, FlattenToRgba(page.Bgra));

    public static RasterPage Load(string path)
    {
        using var bitmap = SKBitmap.Decode(path);

        if (bitmap.ColorType == SKColorType.Bgra8888)
        {
            return new RasterPage(bitmap.Width, bitmap.Height, [.. bitmap.Bytes]);
        }

        var info = new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        using var converted = new SKBitmap(info);
        bitmap.CopyTo(converted, SKColorType.Bgra8888);
        return new RasterPage(converted.Width, converted.Height, [.. converted.Bytes]);
    }

    private static byte[] FlattenToRgba(byte[] bgra)
    {
        var rgba = new byte[bgra.Length];
        for (var i = 0; i < bgra.Length; i += 4)
        {
            var alpha = bgra[i + 3];
            rgba[i] = OnWhite(bgra[i + 2], alpha);
            rgba[i + 1] = OnWhite(bgra[i + 1], alpha);
            rgba[i + 2] = OnWhite(bgra[i], alpha);
            rgba[i + 3] = 255;
        }

        return rgba;
    }

    private static byte OnWhite(byte value, byte alpha) =>
        (byte)(((value * alpha) + (255 * (255 - alpha))) / 255);
}
