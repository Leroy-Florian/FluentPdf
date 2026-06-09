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

    [Fact]
    public void Output_marks_page_boundaries()
    {
        var document = PdfDocumentBuilder.Create().Section(s => s.Paragraph("x")).Build().Value;

        RenderText(document).Should().Contain("[page 1]");
    }

    [Fact]
    public void A_footer_page_number_field_is_resolved()
    {
        var document = PdfDocumentBuilder.Create()
            .Section(s => s
                .Footer(f => f.PageNumber("Page {page} of {pages}"))
                .Paragraph("body"))
            .Build()
            .Value;

        RenderText(document).Should().Contain("Page 1 of 1");
    }

    [Fact]
    public void A_paragraph_longer_than_a_page_paginates_onto_several_pages()
    {
        var longText = string.Join(" ", Enumerable.Repeat("clause", 4000));
        var document = PdfDocumentBuilder.Create()
            .Section(s => s.Paragraph(longText))
            .Build()
            .Value;

        Renderer.Render(document).Value.PageCount.Should().BeGreaterThan(1);
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

    [Fact]
    public void Renders_byte_identically_under_parallel_load()
    {
        // The in-memory renderer is fully deterministic, so the strongest possible proof of
        // thread-safety: a single shared instance hammered from every core must produce output
        // byte-identical to a serial render. Any shared mutable state would corrupt some of them.
        var document = PdfDocumentBuilder.Create()
            .Section(s => s
                .Paragraph("alpha")
                .PageBreak().Paragraph("beta")
                .PageBreak().Paragraph("gamma"))
            .Build()
            .Value;

        var baseline = Renderer.Render(document).Value.ToArray();

        var outputs = new byte[256][];
        Parallel.For(0, outputs.Length, i => outputs[i] = Renderer.Render(document).Value.ToArray());

        outputs.Should().OnlyContain(output => output.SequenceEqual(baseline),
            "concurrent renders of one document on a shared renderer must be byte-identical");
    }

    [Fact]
    public void Does_not_grow_the_managed_heap_across_many_renders()
    {
        // Mass printing renders thousands of documents through one renderer. Because nothing is
        // retained between calls, the managed heap must return to its baseline — this guards
        // against a future change that caches or accumulates per-render state and leaks.
        var document = PdfDocumentBuilder.Create()
            .Section(s => s
                .Paragraph("warm up the pipeline")
                .PageBreak().Paragraph("second page"))
            .Build()
            .Value;

        _ = Renderer.Render(document).Value.PageCount;

        var before = GC.GetTotalMemory(forceFullCollection: true);

        for (var i = 0; i < 5_000; i++)
        {
            _ = Renderer.Render(document).Value.PageCount;
        }

        var after = GC.GetTotalMemory(forceFullCollection: true);

        (after - before).Should().BeLessThan(2 * 1024 * 1024,
            "rendering keeps no per-call state, so repeated renders must not accumulate memory");
    }
}
