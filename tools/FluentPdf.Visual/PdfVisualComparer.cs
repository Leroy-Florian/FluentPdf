namespace FluentPdf.Visual;

/// <summary>The comparison of a single page rendered by two documents.</summary>
public sealed record PageComparison(
    int PageNumber,
    int Width,
    int Height,
    long DifferentPixels,
    double Similarity,
    byte[] DiffPng);

/// <summary>The full result of comparing two PDFs page by page.</summary>
public sealed record VisualComparisonReport(
    int PagesA,
    int PagesB,
    IReadOnlyList<PageComparison> Pages)
{
    /// <summary>Whether both documents produced the same number of pages.</summary>
    public bool PageCountMatches => PagesA == PagesB;

    /// <summary>The mean per-page similarity over the compared pages, in [0, 1].</summary>
    public double MeanSimilarity =>
        Pages.Count == 0 ? 0d : Pages.Average(page => page.Similarity);
}

/// <summary>
/// A visual comparison engine for two PDFs. It rasterises both at identical dimensions and
/// compares each page pixel by pixel, yielding a per-page similarity score and a diff heatmap
/// (matching pixels faded to grey, differing pixels painted red). Use it to confirm that the
/// output of a real adapter is laid out as expected — against a golden baseline for regression,
/// or against another adapter for review.
/// </summary>
public sealed class PdfVisualComparer(int colorTolerance = 48)
{
    private readonly IPdfRasterizer _rasterizer = new PdfRasterizer();

    public PdfVisualComparer(IPdfRasterizer rasterizer, int colorTolerance = 48)
        : this(colorTolerance) => _rasterizer = rasterizer;

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

    /// <summary>Compares two already-rasterised pages, producing a similarity score and diff.</summary>
    public PageComparison ComparePages(int number, RasterPage a, RasterPage b)
    {
        var width = Math.Min(a.Width, b.Width);
        var height = Math.Min(a.Height, b.Height);
        var diff = new byte[width * height * 4];

        long different = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var ia = ((y * a.Width) + x) * 4;
                var ib = ((y * b.Width) + x) * 4;

                // Flatten over white before comparing, so partial/zero alpha (and the PNG
                // round-trip of a golden baseline) cannot create spurious differences — we
                // compare what the page would look like printed on white paper.
                var delta = Math.Abs(OnWhite(a.Bgra, ia) - OnWhite(b.Bgra, ib))
                    + Math.Abs(OnWhite(a.Bgra, ia + 1) - OnWhite(b.Bgra, ib + 1))
                    + Math.Abs(OnWhite(a.Bgra, ia + 2) - OnWhite(b.Bgra, ib + 2));

                var o = ((y * width) + x) * 4;

                if (delta > colorTolerance)
                {
                    different++;
                    diff[o] = 255;     // R
                    diff[o + 1] = 0;   // G
                    diff[o + 2] = 0;   // B
                }
                else
                {
                    // Fade matching content to light grey so differences stand out.
                    var grey = (byte)(220 + (a.Bgra[ia + 2] / 8));
                    diff[o] = grey;
                    diff[o + 1] = grey;
                    diff[o + 2] = grey;
                }

                diff[o + 3] = 255; // A
            }
        }

        var total = (long)width * height;
        var similarity = total == 0 ? 1d : 1d - ((double)different / total);

        return new PageComparison(
            number,
            width,
            height,
            different,
            similarity,
            PngImage.EncodeRgba(width, height, diff));
    }

    /// <summary>Composites one BGRA channel over a white background using the pixel's alpha.</summary>
    private static int OnWhite(byte[] bgra, int channelIndex)
    {
        var alpha = bgra[(channelIndex & ~3) + 3];
        var value = bgra[channelIndex];
        return ((value * alpha) + (255 * (255 - alpha))) / 255;
    }
}
