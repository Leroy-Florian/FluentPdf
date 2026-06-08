using FluentPdf.Visual;

namespace FluentPdf.Visual.UnitTests;

public sealed class ImageMetricsAndBandsTests
{
    [Fact]
    public void Ssim_of_identical_images_is_one()
    {
        var image = Filled(8, 8, 120);

        ImageMetrics.Ssim(image, (byte[])image.Clone(), 8, 0, 8).Should().Be(1d);
    }

    [Fact]
    public void Ssim_drops_when_images_differ()
    {
        var dark = Filled(8, 8, 60);
        var light = Filled(8, 8, 220);

        ImageMetrics.Ssim(dark, light, 8, 0, 8).Should().BeLessThan(0.9d);
    }

    [Fact]
    public void A_localised_change_only_drifts_its_own_band()
    {
        const int width = 60;
        const int height = 120;

        var reference = WhitePage(width, height);
        var changed = WhitePage(width, height);
        PaintBlack(changed, width, top: 50, bottom: 60); // lands in band index 5 (rows 50-59)

        var report = new PdfVisualComparer(bands: 12).ComparePages(1, reference, changed);

        var dirty = report.Bands.Single(band => band.Top <= 50 && band.Bottom > 50);
        dirty.Ssim.Should().BeLessThan(0.99d);
        dirty.PixelSimilarity.Should().BeLessThan(1d);

        // Bands far from the change remain identical.
        report.Bands.First().Ssim.Should().Be(1d);
        report.Bands.Last().Ssim.Should().Be(1d);
    }

    private static byte[] Filled(int width, int height, byte value)
    {
        var data = new byte[width * height];
        Array.Fill(data, value);
        return data;
    }

    private static RasterPage WhitePage(int width, int height)
    {
        var bgra = new byte[width * height * 4];
        Array.Fill(bgra, (byte)255);
        return new RasterPage(width, height, bgra);
    }

    private static void PaintBlack(RasterPage page, int width, int top, int bottom)
    {
        for (var y = top; y < bottom; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = ((y * width) + x) * 4;
                page.Bgra[i] = 0;
                page.Bgra[i + 1] = 0;
                page.Bgra[i + 2] = 0;
                page.Bgra[i + 3] = 255;
            }
        }
    }
}
