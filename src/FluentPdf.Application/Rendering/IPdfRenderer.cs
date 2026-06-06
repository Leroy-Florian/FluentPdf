using FluentPdf.Domain;
using FluentPdf.Kernel;

namespace FluentPdf.Application.Rendering;

/// <summary>
/// The driven port through which the agnostic document model is turned into a concrete
/// PDF. Each PDF library (QuestPDF, iText, PDFsharp, …) is integrated by implementing this
/// interface in its own adapter package; the core never references any of them.
/// </summary>
/// <remarks>
/// An implementation must:
/// <list type="bullet">
/// <item>accurately describe what it supports via <see cref="Descriptor"/>, and</item>
/// <item>return a failed <see cref="Result{T}"/> (never throw) for content it cannot
/// render, so divergence between libraries is explicit.</item>
/// </list>
/// Conformance to these rules is verified by the shared adapter contract test suite.
/// </remarks>
public interface IPdfRenderer
{
    /// <summary>Identifies the adapter and the features it can render.</summary>
    RendererDescriptor Descriptor { get; }

    /// <summary>Renders the document model to encoded PDF bytes.</summary>
    Result<RenderedPdf> Render(PdfDocument document);
}
