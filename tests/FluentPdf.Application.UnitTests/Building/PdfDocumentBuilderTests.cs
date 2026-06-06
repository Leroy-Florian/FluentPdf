using FluentPdf.Application.Building;
using FluentPdf.Application.Rendering;
using FluentPdf.Domain;
using FluentPdf.Domain.Content;
using FluentPdf.Domain.Layout;
using FluentPdf.Domain.Styling;

namespace FluentPdf.Application.UnitTests.Building;

public sealed class PdfDocumentBuilderTests
{
    [Fact]
    public void Builds_a_rich_document_with_every_block_type()
    {
        var result = PdfDocumentBuilder.Create()
            .Metadata(m => m.Title("T").Author("A").Subject("S").Creator("C").Keywords("k1", "k2"))
            .Section(s => s
                .PageSize(PageSize.A4)
                .Landscape()
                .Margins(24d)
                .Header(h => h.Paragraph("header"))
                .Footer(f => f.Paragraph("footer"))
                .Paragraph("plain")
                .Paragraph(p => p.Text("a").Bold("b").Italic("c").Align(HorizontalAlignment.Center))
                .Row(r => r.Column(6, c => c.Paragraph("l")).Column(6, c => c.Paragraph("r")))
                .OrderedList(l => l.Item("one").Item(i => i.Paragraph("two")))
                .Table(t => t.Columns(2).HeaderRow(r => r.Cell("h1").Cell("h2")).Row(r => r.Cell("a").Cell(c => c.Paragraph("b"))))
                .Image([1, 2, 3], ImageFormat.Png, 10d, 10d)
                .Spacer(5d)
                .PageBreak())
            .Build();

        result.IsSuccess.Should().BeTrue();
        var document = result.Value;
        document.Metadata.Title.Should().Be("T");
        document.Metadata.Keywords.Should().Equal("k1", "k2");
        document.Sections.Should().ContainSingle();

        var section = document.Sections[0];
        section.PageSize.Orientation.Should().Be(PageOrientation.Landscape);
        section.Header.Should().NotBeNull();
        section.Footer.Should().NotBeNull();
        section.Blocks.Should().HaveCount(8);

        var features = DocumentFeatureScanner.Scan(document);
        features.Should().Be(RendererCapabilities.Everything);
    }

    [Fact]
    public void Defaults_to_a4_portrait()
    {
        var section = PdfDocumentBuilder.Create()
            .Section(s => s.Paragraph("x"))
            .Build()
            .Value
            .Sections[0];

        section.PageSize.Should().Be(PageSize.A4);
        section.PageSize.Orientation.Should().Be(PageOrientation.Portrait);
    }

    [Fact]
    public void Building_with_no_sections_fails()
    {
        PdfDocumentBuilder.Create().Build().Error
            .Should().Be(DomainErrors.Document.NoSections);
    }

    [Fact]
    public void An_empty_section_fails()
    {
        PdfDocumentBuilder.Create()
            .Section(_ => { })
            .Build()
            .Error.Should().Be(DomainErrors.Section.NoBlocks);
    }

    [Fact]
    public void Invalid_paragraph_text_propagates_as_a_failure()
    {
        PdfDocumentBuilder.Create()
            .Section(s => s.Paragraph(""))
            .Build()
            .Error.Should().Be(DomainErrors.TextRun.EmptyText);
    }

    [Fact]
    public void Invalid_spacer_propagates_as_a_failure()
    {
        PdfDocumentBuilder.Create()
            .Section(s => s.Spacer(0d))
            .Build()
            .Error.Should().Be(DomainErrors.Spacer.NonPositiveHeight);
    }

    [Fact]
    public void Invalid_column_width_propagates_from_the_grid()
    {
        PdfDocumentBuilder.Create()
            .Section(s => s.Row(r => r.Column(99, c => c.Paragraph("x"))))
            .Build()
            .Error.Should().Be(DomainErrors.Grid.ColumnWidthOutOfRange);
    }

    [Fact]
    public void Row_overflow_propagates()
    {
        PdfDocumentBuilder.Create()
            .Section(s => s.Row(r => r
                .Column(8, c => c.Paragraph("x"))
                .Column(8, c => c.Paragraph("y"))))
            .Build()
            .Error.Should().Be(DomainErrors.Grid.RowOverflow);
    }

    [Fact]
    public void Table_width_mismatch_propagates()
    {
        PdfDocumentBuilder.Create()
            .Section(s => s.Table(t => t.Columns(2).Row(r => r.Cell("only one"))))
            .Build()
            .Error.Should().Be(DomainErrors.Table.RowWidthMismatch);
    }

    [Fact]
    public void Invalid_header_content_propagates()
    {
        PdfDocumentBuilder.Create()
            .Section(s => s.Header(h => h.Paragraph("")).Paragraph("body"))
            .Build()
            .Error.Should().Be(DomainErrors.TextRun.EmptyText);
    }

    [Fact]
    public void Negative_uniform_margin_propagates()
    {
        PdfDocumentBuilder.Create()
            .Section(s => s.Margins(-5d).Paragraph("x"))
            .Build()
            .Error.Should().Be(DomainErrors.Margins.NegativeValue);
    }

    [Fact]
    public void Adding_a_null_block_fails()
    {
        PdfDocumentBuilder.Create()
            .Section(s => s.Add(null!))
            .Build()
            .IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Prebuilt_blocks_can_be_spliced_in()
    {
        var reusable = new IBlock[]
        {
            Paragraph.FromText("reused-1").Value,
            Paragraph.FromText("reused-2").Value,
        };

        var section = PdfDocumentBuilder.Create()
            .Section(s => s.Blocks(reusable))
            .Build()
            .Value
            .Sections[0];

        section.Blocks.Should().HaveCount(2);
    }
}
