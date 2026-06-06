namespace FluentPdf.Visual;

/// <summary>Rasterises PDF bytes into per-page pixel buffers.</summary>
public interface IPdfRasterizer
{
    /// <summary>Rasterises every page of the given PDF.</summary>
    IReadOnlyList<RasterPage> Rasterize(byte[] pdf);
}
