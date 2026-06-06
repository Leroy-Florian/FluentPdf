using FluentPdf.Domain;
using FluentPdf.Kernel;

namespace FluentPdf.Application.Building;

/// <summary>
/// The entry point of the fluent API. Assembles a library-agnostic <see cref="PdfDocument"/>
/// from metadata and one or more sections. Errors are accumulated and surfaced as a single
/// failed <see cref="Result{T}"/> from <see cref="Build"/> — the API never throws for
/// invalid content.
/// </summary>
/// <example>
/// <code>
/// var result = PdfDocumentBuilder.Create()
///     .Metadata(m => m.Title("Invoice").Author("ACME"))
///     .Section(s => s
///         .Header(h => h.Paragraph("ACME Corp"))
///         .Paragraph("Thank you for your order.")
///         .Row(r => r
///             .Column(6, c => c.Paragraph("Left"))
///             .Column(6, c => c.Paragraph("Right")))
///         .Component(lineItems, orderDto))
///     .Build();
/// </code>
/// </example>
public sealed class PdfDocumentBuilder
{
    private readonly List<Result<Section>> _sections = [];
    private DocumentMetadata _metadata = DocumentMetadata.Empty;

    private PdfDocumentBuilder()
    {
    }

    /// <summary>Creates a new builder.</summary>
    public static PdfDocumentBuilder Create() => new();

    /// <summary>Configures document-level metadata.</summary>
    public PdfDocumentBuilder Metadata(Action<MetadataBuilder> configure)
    {
        var builder = new MetadataBuilder();
        configure(builder);
        _metadata = builder.Build();
        return this;
    }

    /// <summary>Adds a section to the document.</summary>
    public PdfDocumentBuilder Section(Action<SectionBuilder> configure)
    {
        var builder = new SectionBuilder();
        configure(builder);
        _sections.Add(builder.Build());
        return this;
    }

    /// <summary>Materialises the document, returning the first error if any step failed.</summary>
    public Result<PdfDocument> Build()
    {
        var sections = ResultList.Collect(_sections);

        if (sections.IsFailure)
        {
            return sections.Error;
        }

        return PdfDocument.Create(_metadata, sections.Value);
    }
}
