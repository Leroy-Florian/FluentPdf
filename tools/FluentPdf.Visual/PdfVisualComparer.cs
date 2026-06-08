namespace FluentPdf.Visual;

/// <summary>The comparison of one horizontal band (zone) of a page.</summary>
public sealed record BandComparison(
    int Index,
    int Top,
    int Bottom,
    double PixelSimilarity,
    double Ssim);

/// <summary>The comparison of a single page rendered by two documents.</summary>
public sealed record PageComparison(
    int PageNumber,
    int Width,
    int Height,
    long DifferentPixels,
    double Similarity,
    double Ssim,
    IReadOnlyList<BandComparison> Bands,
    byte[] DiffPng);

/// <summary>The full result of comparing two PDFs page by page.</summary>
public sealed record VisualComparisonReport(
    int PagesA,
    int PagesB,
    IReadOnlyList<PageComparison> Pages)
{
    /// <summary>Whether both documents produced the same number of pages.</summary>
    public bool PageCountMatches => PagesA == PagesB;

    /// <summary>The mean per-page pixel similarity over the compared pages, in [0, 1].</summary>
    public double MeanSimilarity =>
        Pages.Count == 0 ? 0d : Pages.Average(page => page.Similarity);

    /// <summary>The mean per-page SSIM over the compared pages, in [0, 1].</summary>
    public double MeanSsim =>
        Pages.Count == 0 ? 0d : Pages.Average(page => page.Ssim);
}

/// <summary>
/// A visual comparison engine for two PDFs. It rasterises both at identical dimensions and
/// compares each page, yielding a pixel-difference similarity, a perceptual <b>SSIM</b> score,
/// a per-band (zone) breakdown and a diff heatmap (matching pixels faded to grey, differing
/// pixels painted red). Use it for golden-baseline regression, or for adapter review.
/// </summary>
public sealed class PdfVisualComparer(int colorTolerance = 48, int bands = 12)
{
    private readonly IPdfRasterizer _rasterizer = new PdfRasterizer();
    private readonly int _bands = Math.Max(1, bands);

    public PdfVisualComparer(IPdfRasterizer rasterizer, int colorTolerance = 48, int bands = 12)
        : this(colorTolerance, bands) => _rasterizer = rasterizer;

    /// <summary>Rasterises and compares two PDFs page by page.</summary>
    public VisualComparisonReport Compare(byte[] expected, byte[] actual)
    {
        var pagesA = _rasterizer.Rasterize(expected);
        var pagesB = _rasterizer.Rasterize(actual);

        var common = Math.Min(pagesA.Count, pagesB.Count);
        var comparisons = new List<PageComparison>(common);

        for (var i = 0; i < common; i++)
        {
            comparisons.Add(ComparePages(i + 1, pagesA[i], pagesB[i]));
        }

        return new VisualComparisonReport(pagesA.Count, pagesB.Count, comparisons);
    }

    /// <summary>Compares two already-rasterised pages (pixel similarity, SSIM, bands, diff).</summary>
    public PageComparison ComparePages(int number, RasterPage a, RasterPage b)
    {
        var width = Math.Min(a.Width, b.Width);
        var height = Math.Min(a.Height, b.Height);

        var diff = new byte[width * height * 4];
        var greyA = new byte[width * height];
        var greyB = new byte[width * height];

        var bandHeight = Math.Max(1, (int)Math.Ceiling(height / (double)_bands));
        var bandCount = Math.Min(_bands, (int)Math.Ceiling(height / (double)bandHeight));
        var bandDifferent = new long[Math.Max(bandCount, 1)];
        var bandPixels = new long[Math.Max(bandCount, 1)];

        long different = 0;

        for (var y = 0; y < height; y++)
        {
            var band = Math.Min(bandCount - 1, y / bandHeight);

            for (var x = 0; x < width; x++)
            {
                var ia = ((y * a.Width) + x) * 4;
                var ib = ((y * b.Width) + x) * 4;

                // Flatten over white so partial/zero alpha (and a golden's PNG round-trip)
                // cannot create spurious differences — compare the page as printed on white.
                int ra = OnWhite(a.Bgra, ia + 2), ga = OnWhite(a.Bgra, ia + 1), ba = OnWhite(a.Bgra, ia);
                int rb = OnWhite(b.Bgra, ib + 2), gb = OnWhite(b.Bgra, ib + 1), bb = OnWhite(b.Bgra, ib);

                var o = (y * width) + x;
                greyA[o] = Luminance(ra, ga, ba);
                greyB[o] = Luminance(rb, gb, bb);

                var delta = Math.Abs(ra - rb) + Math.Abs(ga - gb) + Math.Abs(ba - bb);
                var p = o * 4;
                bandPixels[band]++;

                if (delta > colorTolerance)
                {
                    different++;
                    bandDifferent[band]++;
                    diff[p] = 255;
                    diff[p + 1] = 0;
                    diff[p + 2] = 0;
                }
                else
                {
                    var grey = (byte)(220 + (ra / 8));
                    diff[p] = grey;
                    diff[p + 1] = grey;
                    diff[p + 2] = grey;
                }

                diff[p + 3] = 255;
            }
        }

        var total = (long)width * height;
        var similarity = total == 0 ? 1d : 1d - ((double)different / total);
        var ssim = ImageMetrics.Ssim(greyA, greyB, width, 0, height);
        var bandResults = BuildBands(greyA, greyB, width, height, bandHeight, bandCount, bandDifferent, bandPixels);

        return new PageComparison(
            number,
            width,
            height,
            different,
            similarity,
            ssim,
            bandResults,
            PngImage.EncodeRgba(width, height, diff));
    }

    private static List<BandComparison> BuildBands(
        byte[] greyA,
        byte[] greyB,
        int width,
        int height,
        int bandHeight,
        int bandCount,
        long[] bandDifferent,
        long[] bandPixels)
    {
        var results = new List<BandComparison>(bandCount);

        for (var band = 0; band < bandCount; band++)
        {
            var top = band * bandHeight;
            var bottom = Math.Min(height, top + bandHeight);
            var pixels = bandPixels[band];
            var pixelSimilarity = pixels == 0 ? 1d : 1d - ((double)bandDifferent[band] / pixels);
            var ssim = ImageMetrics.Ssim(greyA, greyB, width, top, bottom);

            results.Add(new BandComparison(band, top, bottom, pixelSimilarity, ssim));
        }

        return results;
    }

    private static byte Luminance(int r, int g, int b) =>
        (byte)(((r * 299) + (g * 587) + (b * 114)) / 1000);

    /// <summary>Composites one BGRA channel over a white background using the pixel's alpha.</summary>
    private static int OnWhite(byte[] bgra, int channelIndex)
    {
        var alpha = bgra[(channelIndex & ~3) + 3];
        var value = bgra[channelIndex];
        return ((value * alpha) + (255 * (255 - alpha))) / 255;
    }
}
