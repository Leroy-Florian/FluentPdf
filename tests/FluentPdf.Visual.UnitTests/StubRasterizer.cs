using FluentPdf.Visual;

namespace FluentPdf.Visual.UnitTests;

/// <summary>
/// A hand-written <see cref="IPdfRasterizer"/> fake that returns predetermined pages, so the
/// comparison logic can be tested with synthetic pixels (no real PDF or PDFium involved).
/// </summary>
internal sealed class StubRasterizer(RasterPage expected, RasterPage actual) : IPdfRasterizer
{
    public IReadOnlyList<RasterPage> Rasterize(byte[] pdf) => pdf[0] == 1 ? [expected] : [actual];
}
