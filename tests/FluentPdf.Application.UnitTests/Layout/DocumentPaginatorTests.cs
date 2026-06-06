using FluentPdf.Application.Building;
using FluentPdf.Application.Layout;
using FluentPdf.Domain;
using FluentPdf.Domain.Content;
using FluentPdf.Domain.Styling;

namespace FluentPdf.Application.UnitTests.Layout;

public sealed class DocumentPaginatorTests
{
    private static PdfDocument Document(Action<SectionBuilder> section) =>
        PdfDocumentBuilder.Create().Section(section).Build().Value;

    private static string LongText(int words) => string.Join(" ", Enumerable.Repeat("lorem", words));

    [Fact]
    public void A_null_measurer_is_rejected()
    {
        var act = () => new DocumentPaginator(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Atomic_blocks_overflow_onto_fresh_pages()
    {
        // A4 portrait with no margins ≈ 841pt tall; two 400pt spacers fit, the third overflows.
        var document = Document(s => s
            .Margins(0d)
            .Spacer(400d).Spacer(400d).Spacer(400d).Spacer(400d).Spacer(400d));

        new DocumentPaginator(new FixedTextMeasurer()).CountPages(document).Should().Be(3);
    }

    [Fact]
    public void Explicit_page_breaks_are_honoured()
    {
        var document = Document(s => s
            .Paragraph("one").PageBreak().Paragraph("two").PageBreak().Paragraph("three"));

        new DocumentPaginator(new FixedTextMeasurer()).CountPages(document).Should().Be(3);
    }

    [Fact]
    public void A_long_paragraph_splits_across_pages_without_losing_text()
    {
        var text = LongText(400);
        var document = Document(s => s.Margins(0d).Paragraph(text));
        var paginator = new DocumentPaginator(new FixedTextMeasurer(lineHeight: 200d));

        var pages = paginator.Paginate(document).ToList();

        pages.Count.Should().BeGreaterThan(1);

        var reconstructed = string.Join(
            " ",
            pages.Select(page => ((Paragraph)page.Body[0]).Runs[0].Text));

        reconstructed.Should().Be(text);
    }

    [Fact]
    public void A_long_list_splits_at_item_boundaries_keeping_numbering_continuous()
    {
        var document = Document(s =>
        {
            s.Margins(0d);
            s.OrderedList(l =>
            {
                for (var i = 1; i <= 10; i++)
                {
                    l.Item($"item {i}");
                }
            });
        });

        var pages = new DocumentPaginator(new FixedTextMeasurer(lineHeight: 200d))
            .Paginate(document)
            .ToList();

        pages.Count.Should().BeGreaterThan(1);

        var lists = pages.Select(page => (ListBlock)page.Body[0]).ToList();
        lists.Sum(list => list.Items.Count).Should().Be(10);

        var expectedStart = 1;
        foreach (var list in lists)
        {
            list.StartNumber.Should().Be(expectedStart);
            expectedStart += list.Items.Count;
        }
    }

    [Fact]
    public void A_tall_grid_row_splits_each_column_at_the_cut()
    {
        var document = Document(s => s
            .Margins(0d)
            .Row(r => r
                .Column(6, c =>
                {
                    for (var i = 1; i <= 8; i++)
                    {
                        c.Paragraph($"L{i}");
                    }
                })
                .Column(6, c =>
                {
                    for (var i = 1; i <= 8; i++)
                    {
                        c.Paragraph($"R{i}");
                    }
                })));

        var pages = new DocumentPaginator(new FixedTextMeasurer(lineHeight: 200d))
            .Paginate(document)
            .ToList();

        pages.Count.Should().BeGreaterThan(1);

        foreach (var page in pages)
        {
            var row = (RowBlock)page.Body[0];
            row.Columns.Should().HaveCount(2);
            row.Columns[0].Blocks.Should().NotBeEmpty();
            row.Columns[1].Blocks.Should().NotBeEmpty();
        }

        var totalParagraphs = pages
            .Select(page => (RowBlock)page.Body[0])
            .Sum(row => row.Columns[0].Blocks.Count + row.Columns[1].Blocks.Count);
        totalParagraphs.Should().Be(16);
    }

    [Fact]
    public void Page_number_fields_are_resolved_with_the_total()
    {
        var document = PdfDocumentBuilder.Create()
            .Section(s => s
                .Footer(f => f.PageNumber("Page {page} of {pages}", HorizontalAlignment.Center))
                .Paragraph("a").PageBreak().Paragraph("b"))
            .Build()
            .Value;

        var pages = new DocumentPaginator(new FixedTextMeasurer()).Paginate(document).ToList();

        pages.Should().HaveCount(2);
        FooterText(pages[0]).Should().Be("Page 1 of 2");
        FooterText(pages[1]).Should().Be("Page 2 of 2");
    }

    [Fact]
    public void Pagination_is_lazy_and_does_no_work_until_enumerated()
    {
        var document = Document(s => s.Paragraph("content"));
        var paginator = new DocumentPaginator(new ThrowingTextMeasurer());

        var sequence = paginator.Paginate(document); // must not measure yet

        var enumerate = () => sequence.First();
        enumerate.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Streaming_produces_the_first_page_with_less_work_than_the_whole_document()
    {
        var document = Document(s => s.Margins(0d).Paragraph(LongText(2000)));
        var measurer = new FixedTextMeasurer(lineHeight: 200d);
        var paginator = new DocumentPaginator(measurer);

        paginator.Paginate(document).First();
        var afterFirst = measurer.WordMeasurements;

        foreach (var _ in paginator.Paginate(document))
        {
            // drain
        }

        var afterAll = measurer.WordMeasurements;

        afterFirst.Should().BeLessThan(afterAll);
    }

    private static string FooterText(LaidOutPage page) =>
        ((Paragraph)page.Footer[0]).Runs[0].Text;
}
