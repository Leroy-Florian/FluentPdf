using FluentPdf.Domain.Styling;

namespace FluentPdf.Application.Layout;

/// <summary>
/// The measurement port the pagination engine depends on. Adapters supply real font metrics
/// from their own library; the built-in approximate measurer offers a fast, dependency-free
/// default. Pagination is correct <em>relative to the measurer supplied</em>, so an adapter
/// that paginates through this engine must render each page with the same metrics.
/// </summary>
/// <remarks>
/// Methods take a <see cref="ReadOnlySpan{T}"/> so the engine can measure words without
/// allocating substrings on the hot path — essential for high-volume, mass-print workloads.
/// </remarks>
public interface ITextMeasurer
{
    /// <summary>The height of a single line set in the given style, in points (incl. leading).</summary>
    double LineHeight(TextStyle style);

    /// <summary>The advance width of a single inter-word space in the given style, in points.</summary>
    double SpaceWidth(TextStyle style);

    /// <summary>The advance width of a single word (no surrounding spaces), in points.</summary>
    double MeasureWord(ReadOnlySpan<char> word, TextStyle style);
}
