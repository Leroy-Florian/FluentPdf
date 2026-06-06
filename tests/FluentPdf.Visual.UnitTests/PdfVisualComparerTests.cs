using FluentPdf.Visual;

namespace FluentPdf.Visual.UnitTests;

public sealed class PdfVisualComparerTests
{
    [Fact]
    public void Identical_pages_compare_as_fully_similar()
    {
        var page = SolidPage(10, 10, 10, 20, 30);
        var comparison = ComparePages(page, Clone(page));

        comparison.Similarity.Should().Be(1d);
        comparison.DifferentPixels.Should().Be(0);
    }

    [Fact]
    public void Wholly_different_pages_compare_as_dissimilar()
    {
        var black = SolidPage(8, 8, 0, 0, 0);
        var white = SolidPage(8, 8, 255, 255, 255);

        ComparePages(black, white).Similarity.Should().BeLessThan(0.01d);
    }

    [Fact]
    public void Similarity_reflects_the_fraction_of_matching_pixels()
    {
        var a = SolidPage(10, 10, 0, 0, 0);
        var b = SolidPage(10, 10, 0, 0, 0);

        // Flip a quarter of b's pixels to white.
        for (var i = 0; i < 25; i++)
        {
            b.Bgra[(i * 4) + 0] = 255;
            b.Bgra[(i * 4) + 1] = 255;
            b.Bgra[(i * 4) + 2] = 255;
        }

        ComparePages(a, b).Similarity.Should().BeApproximately(0.75d, 0.001d);
    }

    [Fact]
    public void Encoded_diff_is_a_valid_png()
    {
        var diff = ComparePages(SolidPage(4, 4, 0, 0, 0), SolidPage(4, 4, 255, 255, 255)).DiffPng;

        diff.Should().StartWith([(byte)137, (byte)80, (byte)78, (byte)71]);
    }

    [Fact]
    public void Ink_ratio_distinguishes_blank_from_inked_pages()
    {
        SolidPage(10, 10, 255, 255, 255).InkRatio().Should().Be(0d);
        SolidPage(10, 10, 0, 0, 0).InkRatio().Should().Be(1d);
    }

    private static RasterPage SolidPage(int width, int height, byte red, byte green, byte blue)
    {
        var bgra = new byte[width * height * 4];
        for (var i = 0; i < bgra.Length; i += 4)
        {
            bgra[i] = blue;
            bgra[i + 1] = green;
            bgra[i + 2] = red;
            bgra[i + 3] = 255;
        }

        return new RasterPage(width, height, bgra);
    }

    private static RasterPage Clone(RasterPage page) =>
        new(page.Width, page.Height, [.. page.Bgra]);

    private static PageComparison ComparePages(RasterPage a, RasterPage b)
    {
        // Drive the page-level comparison through a tiny single-page in-memory pair by
        // exercising the public comparer via a stub rasterizer.
        var comparer = new PdfVisualComparer(new StubRasterizer(a, b));
        return comparer.Compare([1], [2]).Pages[0];
    }
}
