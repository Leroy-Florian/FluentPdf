using FluentPdf.Domain.Content;

namespace FluentPdf.Application.Rendering;

/// <summary>
/// Renders an agnostic <see cref="ChartBlock"/> to a raster image (PNG) that an adapter can
/// embed. This is an <em>optional</em> port: an adapter given a chart renderer advertises the
/// <see cref="PdfFeature.Chart"/> capability, otherwise it declares charts unsupported (and the
/// use case refuses chart content). Keeping it a port means the charting back-end — and its
/// native dependency (e.g. SkiaSharp) — is only referenced when charts are actually wanted.
/// </summary>
public interface IChartRenderer
{
    /// <summary>Renders the chart to PNG bytes (drawn at, or scaled to, the chart's size).</summary>
    byte[] RenderPng(ChartBlock chart);
}
