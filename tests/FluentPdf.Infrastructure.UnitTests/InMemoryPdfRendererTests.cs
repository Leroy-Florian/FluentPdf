using System.Text;
using FluentPdf.Application.Building;
using FluentPdf.Application.Rendering;
using FluentPdf.Domain;
using FluentPdf.Domain.Content;
using FluentPdf.Infrastructure.Rendering;

namespace FluentPdf.Infrastructure.UnitTests;

public sealed class InMemoryPdfRendererTests
{
    private static readonly InMemoryPdfRenderer Renderer = new();

    private static string RenderText(PdfDocument document) =>
        Encoding.UTF8.GetString(Renderer.Render(document).Value.ToArray());

    [Fact]
    public void Advertises_full_capabilities()
    {
        Renderer.Descriptor.Name.Should().Be("InMemory");
        Renderer.Descriptor.Capabilities.Supports(RendererCapabilities.Everything)
            .Should().BeTrue();
    }

    [Fact]
    public void Rejects_a_null_document()
    {
        Renderer.Render(null!).Error.Should().Be(RenderErrors.NullDocument);
    }

    [Fact]
    public void Output_starts_with_the_pdf_header()
    {
        var document = PdfDocumentBuilder.Create().Section(s => s.Paragraph("x")).Build().Value;

        RenderText(document).Should().StartWith(InMemoryPdfRenderer.Header);
    }

    [Fact]
    public void Output_contains_every_visible_string()
    {
        var document = PdfDocumentBuilder.Create()
            .Metadata(m => m.Title("My Title").Author("Me").Subject("Sub").Creator("App").Keywords("k"))
            .Section(s => s
                .Header(h => h.Paragraph("the-header"))
                .Footer(f => f.Paragraph("the-footer"))
                .Paragraph("body-text")
                .OrderedList(l => l.Item("list-entry"))
                .Table(t => t.Columns(1).Row(r => r.Cell("cell-text")))
                .Row(r => r.Column(12, c => c.Paragraph("col-text")))
                .Image([1], ImageFormat.Png, 4d, 8d))
            .Build()
            .Value;

        var text = RenderText(document);

        text.Should().ContainAll(
            "My Title", "Me", "Sub", "App", "keywords: k",
            "the-header", "the-footer", "body-text",
            "list-entry", "cell-text", "col-text",
            "[image:Png 4x8]");
    }

    [Fact]
    public void Output_describes_a_chart_with_its_categories_and_series()
    {
        var document = PdfDocumentBuilder.Create()
            .Section(s => s.Chart(c => c
                .Line()
                .Title("chart-title")
                .Categories("Q1", "Q2")
                .Series("Revenue", 100d, 110d)))
            .Build()
            .Value;

        var text = RenderText(document);

        text.Should().ContainAll(
            "[chart:Line", "chart-title",
            "categories: Q1, Q2",
            "series Revenue: 100, 110");
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 3)]
    public void Counts_one_page_per_section_plus_page_breaks(int pageBreaks, int expectedPages)
    {
        var document = PdfDocumentBuilder.Create()
            .Section(s =>
            {
                s.Paragraph("start");
                for (var i = 0; i < pageBreaks; i++)
                {
                    s.PageBreak();
                }
            })
            .Build()
            .Value;

        Renderer.Render(document).Value.PageCount.Should().Be(expectedPages);
    }
}
