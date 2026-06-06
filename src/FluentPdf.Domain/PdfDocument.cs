using FluentPdf.Domain.Events;
using FluentPdf.Kernel;

namespace FluentPdf.Domain;

/// <summary>
/// The aggregate root: a complete, library-agnostic description of a PDF document as an
/// ordered sequence of <see cref="Section"/>s plus document-level metadata. Adapters
/// translate this model into a concrete PDF using whatever rendering library they wrap.
/// </summary>
public sealed class PdfDocument : AggregateRoot<DocumentId>
{
    private PdfDocument(DocumentId id, DocumentMetadata metadata, IReadOnlyList<Section> sections)
        : base(id)
    {
        Metadata = metadata;
        Sections = sections;
    }

    public DocumentMetadata Metadata { get; }

    public IReadOnlyList<Section> Sections { get; }

    /// <summary>
    /// Assembles a document from its sections and metadata. A document must contain at
    /// least one section. A fresh identifier is generated.
    /// </summary>
    public static Result<PdfDocument> Create(
        DocumentMetadata metadata,
        IReadOnlyList<Section> sections)
    {
        if (metadata is null)
        {
            return Error.NullValue;
        }

        if (sections is null || sections.Count == 0)
        {
            return DomainErrors.Document.NoSections;
        }

        if (sections.Any(static section => section is null))
        {
            return Error.NullValue;
        }

        var document = new PdfDocument(DocumentId.New(), metadata, [.. sections]);
        document.RaiseDomainEvent(new DocumentCreatedEvent(document.Id, document.Sections.Count));

        return document;
    }
}
