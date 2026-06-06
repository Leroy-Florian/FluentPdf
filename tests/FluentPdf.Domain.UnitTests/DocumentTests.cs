using FluentPdf.Domain;
using FluentPdf.Domain.Content;
using FluentPdf.Domain.Events;
using FluentPdf.Domain.Layout;

namespace FluentPdf.Domain.UnitTests;

public sealed class DocumentTests
{
    private static Paragraph SampleParagraph() => Paragraph.FromText("hello").Value;

    private static Section SampleSection() =>
        Section.Create(PageSize.A4, Margins.None, [SampleParagraph()]).Value;

    [Fact]
    public void DocumentId_new_is_non_empty()
    {
        DocumentId.New().Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void DocumentId_create_rejects_empty_guid()
    {
        DocumentId.Create(Guid.Empty).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void DocumentId_compares_by_value()
    {
        var guid = Guid.NewGuid();

        DocumentId.Create(guid).Value.Should().Be(DocumentId.Create(guid).Value);
    }

    [Fact]
    public void Metadata_empty_has_no_values()
    {
        DocumentMetadata.Empty.Title.Should().BeNull();
        DocumentMetadata.Empty.Keywords.Should().BeEmpty();
    }

    [Fact]
    public void Section_requires_blocks()
    {
        Section.Create(PageSize.A4, Margins.None, []).Error
            .Should().Be(DomainErrors.Section.NoBlocks);
    }

    [Fact]
    public void Section_keeps_optional_header_and_footer()
    {
        var furniture = PageFurniture.Create([SampleParagraph()]).Value;

        var section = Section.Create(
            PageSize.A4,
            Margins.None,
            [SampleParagraph()],
            header: furniture,
            footer: furniture).Value;

        section.Header.Should().BeSameAs(furniture);
        section.Footer.Should().BeSameAs(furniture);
    }

    [Fact]
    public void Document_requires_at_least_one_section()
    {
        PdfDocument.Create(DocumentMetadata.Empty, []).Error
            .Should().Be(DomainErrors.Document.NoSections);
    }

    [Fact]
    public void Document_rejects_null_sections_in_the_list()
    {
        PdfDocument.Create(DocumentMetadata.Empty, [null!]).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Document_is_created_with_a_unique_id_and_sections()
    {
        var document = PdfDocument.Create(DocumentMetadata.Empty, [SampleSection()]).Value;

        document.Id.Value.Should().NotBe(Guid.Empty);
        document.Sections.Should().ContainSingle();
    }

    [Fact]
    public void Creating_a_document_raises_a_created_event()
    {
        var document = PdfDocument.Create(DocumentMetadata.Empty, [SampleSection()]).Value;

        var @event = document.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DocumentCreatedEvent>().Subject;

        @event.DocumentId.Should().Be(document.Id);
        @event.SectionCount.Should().Be(1);
    }
}
