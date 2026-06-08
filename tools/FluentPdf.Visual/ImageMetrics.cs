namespace FluentPdf.Visual;

/// <summary>
/// Image similarity metrics over 8-bit greyscale buffers. SSIM (Structural Similarity Index)
/// compares luminance, contrast and structure in local windows, so it tolerates imperceptible
/// pixel-level noise while still catching real layout changes — a better release-gate signal
/// than a raw pixel-difference count.
/// </summary>
public static class ImageMetrics
{
    // SSIM stabilisation constants for 8-bit data (L = 255): C1 = (0.01·L)², C2 = (0.03·L)².
    private const double C1 = 6.5025d;
    private const double C2 = 58.5225d;
    private const int Block = 8;

    /// <summary>
    /// Mean SSIM over <paramref name="block"/>-sized windows between two greyscale images of the
    /// same width, restricted to the row range [<paramref name="top"/>, <paramref name="bottom"/>).
    /// Returns 1.0 for identical input.
    /// </summary>
    public static double Ssim(
        byte[] a,
        byte[] b,
        int width,
        int top,
        int bottom,
        int block = Block)
    {
        var sum = 0d;
        var windows = 0;

        for (var by = top; by < bottom; by += block)
        {
            var rows = Math.Min(block, bottom - by);

            for (var bx = 0; bx < width; bx += block)
            {
                var cols = Math.Min(block, width - bx);
                sum += WindowSsim(a, b, width, bx, by, cols, rows);
                windows++;
            }
        }

        return windows == 0 ? 1d : sum / windows;
    }

    private static double WindowSsim(byte[] a, byte[] b, int width, int x0, int y0, int cols, int rows)
    {
        double sumA = 0, sumB = 0, sumAA = 0, sumBB = 0, sumAB = 0;
        var n = cols * rows;

        for (var y = y0; y < y0 + rows; y++)
        {
            var row = y * width;
            for (var x = x0; x < x0 + cols; x++)
            {
                double va = a[row + x];
                double vb = b[row + x];
                sumA += va;
                sumB += vb;
                sumAA += va * va;
                sumBB += vb * vb;
                sumAB += va * vb;
            }
        }

        var muA = sumA / n;
        var muB = sumB / n;
        var varA = (sumAA / n) - (muA * muA);
        var varB = (sumBB / n) - (muB * muB);
        var cov = (sumAB / n) - (muA * muB);

        var numerator = ((2d * muA * muB) + C1) * ((2d * cov) + C2);
        var denominator = ((muA * muA) + (muB * muB) + C1) * (varA + varB + C2);

        return denominator == 0d ? 1d : numerator / denominator;
    }
}
