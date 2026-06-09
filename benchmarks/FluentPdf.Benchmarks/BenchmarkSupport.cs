using FluentPdf.Application.Rendering;
using iText.Kernel.Pdf;

namespace FluentPdf.Benchmarks;

/// <summary>
/// Helpers that keep the raw-vs-FluentPdf comparison honest: a library-independent page counter
/// and an equivalence guard that fails loudly (in global setup) if a raw baseline drifts away
/// from the document its FluentPdf counterpart produces. A faster-but-different baseline would
/// make the numbers meaningless, so the guard refuses to let the benchmark run in that state.
/// </summary>
internal static class BenchmarkSupport
{
    /// <summary>Counts the pages of a real PDF, whichever library wrote it.</summary>
    public static int PageCount(byte[] pdf)
    {
        using var stream = new MemoryStream(pdf);
        using var reader = new PdfReader(stream);
        using var document = new PdfDocument(reader);
        return document.GetNumberOfPages();
    }

    /// <summary>
    /// Asserts the raw baseline and the FluentPdf render produced non-empty output with the same
    /// page count, so the only thing the benchmark measures is the cost of getting there.
    /// </summary>
    public static void EnsureEquivalent(string label, byte[] raw, RenderedPdf fluent)
    {
        if (raw.Length == 0)
        {
            throw new InvalidOperationException($"{label}: the raw baseline produced no bytes.");
        }

        var rawPages = PageCount(raw);
        if (rawPages != fluent.PageCount)
        {
            throw new InvalidOperationException(
                $"{label}: raw produced {rawPages} page(s) but FluentPdf produced {fluent.PageCount}. "
                + "The documents are not equivalent, so the comparison would be misleading.");
        }
    }
}
