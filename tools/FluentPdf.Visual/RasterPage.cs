namespace FluentPdf.Visual;

/// <summary>
/// A single rasterised PDF page: its pixel dimensions and the raw BGRA bytes (4 bytes per
/// pixel, as produced by PDFium), four bytes per pixel in blue-green-red-alpha order.
/// </summary>
public sealed class RasterPage(int width, int height, byte[] bgra)
{
    public int Width { get; } = width;

    public int Height { get; } = height;

    /// <summary>The raw pixels in BGRA order, length <c>Width * Height * 4</c>.</summary>
    public byte[] Bgra { get; } = bgra;

    /// <summary>
    /// The fraction of pixels that carry visible (non-white, opaque) content, in [0, 1]. A
    /// near-zero value means a blank page, which is a quick "is anything on the page?" check.
    /// </summary>
    public double InkRatio()
    {
        var inked = 0L;
        var pixels = Width * Height;

        for (var i = 0; i < Bgra.Length; i += 4)
        {
            var blue = Bgra[i];
            var green = Bgra[i + 1];
            var red = Bgra[i + 2];
            var alpha = Bgra[i + 3];

            var isWhite = blue >= 250 && green >= 250 && red >= 250;
            if (alpha > 16 && !isWhite)
            {
                inked++;
            }
        }

        return pixels == 0 ? 0d : (double)inked / pixels;
    }
}
