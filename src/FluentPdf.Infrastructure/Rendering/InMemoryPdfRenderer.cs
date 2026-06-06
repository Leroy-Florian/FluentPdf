using System.Text;
using FluentPdf.Application.Layout;
using FluentPdf.Application.Rendering;
using FluentPdf.Domain;
using FluentPdf.Kernel;

namespace FluentPdf.Infrastructure.Rendering;

/// <summary>
/// A reference <see cref="IPdfRenderer"/> that "renders" a document to a deterministic text
/// payload prefixed with a PDF header. It depends on no third-party PDF library, which makes
/// it the proof that the model is genuinely library-agnostic, a fast test double for
/// consumers, and the baseline that exercises the shared adapter conformance suite.
/// </summary>
/// <remarks>
/// It paginates the document through the <see cref="DocumentPaginator"/> and the built-in
/// <see cref="ApproximateTextMeasurer"/>, so its reported page count reflects real content
/// flow (not just explicit breaks) and page-number fields are resolved. Pages are consumed
/// lazily, keeping memory flat for very long documents.
/// </remarks>
public sealed class InMemoryPdfRenderer : IPdfRenderer
{
    /// <summary>The header every produced payload starts with, mirroring a real PDF file.</summary>
    public const string Header = "%PDF-1.7\n";

    private readonly DocumentPaginator _paginator = new(new ApproximateTextMeasurer());

    /// <inheritdoc />
    public RendererDescriptor Descriptor { get; } = new("InMemory", RendererCapabilities.Full);

    /// <inheritdoc />
    public Result<RenderedPdf> Render(PdfDocument document)
    {
        if (document is null)
        {
            return RenderErrors.NullDocument;
        }

        var builder = new StringBuilder();
        builder.Append(Header);

        var pageCount = DocumentTextSerializer.Serialize(builder, document, _paginator.Paginate(document));

        return RenderedPdf.Create(Encoding.UTF8.GetBytes(builder.ToString()), pageCount);
    }
}
