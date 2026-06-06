using System.Text;
using FluentPdf.Application.Rendering;
using FluentPdf.Domain;
using FluentPdf.Domain.Content;
using FluentPdf.Kernel;

namespace FluentPdf.Infrastructure.Rendering;

/// <summary>
/// A reference <see cref="IPdfRenderer"/> that "renders" a document to a deterministic text
/// payload prefixed with a PDF header. It depends on no third-party PDF library, which makes
/// it the proof that the model is genuinely library-agnostic, a fast test double for
/// consumers, and the baseline that exercises the shared adapter conformance suite.
/// </summary>
public sealed class InMemoryPdfRenderer : IPdfRenderer
{
    /// <summary>The header every produced payload starts with, mirroring a real PDF file.</summary>
    public const string Header = "%PDF-1.7\n";

    /// <inheritdoc />
    public RendererDescriptor Descriptor { get; } = new("InMemory", RendererCapabilities.Full);

    /// <inheritdoc />
    public Result<RenderedPdf> Render(PdfDocument document)
    {
        if (document is null)
        {
            return RenderErrors.NullDocument;
        }

        var payload = Header + DocumentTextSerializer.Serialize(document);
        var bytes = Encoding.UTF8.GetBytes(payload);

        return RenderedPdf.Create(bytes, CountPages(document));
    }

    private static int CountPages(PdfDocument document)
    {
        var pages = 0;

        foreach (var section in document.Sections)
        {
            // Each section begins on a fresh page; every top-level page break adds one more.
            pages += 1 + section.Blocks.Count(static block => block is PageBreak);
        }

        return pages;
    }
}
