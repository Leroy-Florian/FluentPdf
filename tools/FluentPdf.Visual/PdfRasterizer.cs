using Docnet.Core;
using Docnet.Core.Models;

namespace FluentPdf.Visual;

/// <summary>
/// Rasterises PDF bytes to per-page pixel buffers using PDFium (Docnet). Both documents in a
/// comparison are rendered at the same fixed dimensions so pages line up pixel-for-pixel.
/// </summary>
public sealed class PdfRasterizer : IPdfRasterizer
{
    /// <summary>A4 portrait at roughly 110 DPI — a good default for visual comparison.</summary>
    public const int DefaultWidth = 908;

    /// <summary>A4 portrait at roughly 110 DPI.</summary>
    public const int DefaultHeight = 1284;

    private readonly int _width;
    private readonly int _height;

    public PdfRasterizer(int width = DefaultWidth, int height = DefaultHeight)
    {
        _width = width;
        _height = height;
    }

    /// <summary>Rasterises every page of <paramref name="pdf"/> at the configured dimensions.</summary>
    public IReadOnlyList<RasterPage> Rasterize(byte[] pdf)
    {
        ArgumentNullException.ThrowIfNull(pdf);

        var pages = new List<RasterPage>();

        using var reader = DocLib.Instance.GetDocReader(pdf, new PageDimensions(_width, _height));
        var count = reader.GetPageCount();

        for (var i = 0; i < count; i++)
        {
            using var pageReader = reader.GetPageReader(i);
            var raw = pageReader.GetImage();
            pages.Add(new RasterPage(pageReader.GetPageWidth(), pageReader.GetPageHeight(), raw));
        }

        return pages;
    }
}
