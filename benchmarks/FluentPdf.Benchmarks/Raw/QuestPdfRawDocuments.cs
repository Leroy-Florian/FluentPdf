using System.Globalization;
using FluentPdf.Adapters.Shared;
using FluentPdf.Samples.Contract;
using FluentPdf.Samples.Invoice;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestDocument = QuestPDF.Fluent.Document;
using QuestMetadata = QuestPDF.Infrastructure.DocumentMetadata;

namespace FluentPdf.Benchmarks.Raw;

/// <summary>
/// Hand-written QuestPDF documents — the code a caller would write if they used QuestPDF
/// directly, without FluentPdf. They reproduce the same documents as the matching samples
/// (same fonts, page sizes, table theme and text), so the benchmark compares like with like
/// and the delta against the FluentPdf pipeline is the cost of the agnostic layer alone.
/// </summary>
/// <remarks>
/// Font registration and the Community licence are owned by <c>QuestPdfRenderer</c>'s static
/// constructor; the benchmark touches that type in its global setup before these run.
/// </remarks>
internal static class QuestPdfRawDocuments
{
    private const string Muted = "#666666";

    public static byte[] Invoice(InvoiceDto invoice) =>
        QuestDocument.Create(container => container.Page(page =>
            {
                ConfigurePage(page, 595.28f, 841.89f, 48f);

                page.Header().Text("ACME Corp — 123 Example Street");

                page.Content().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem(8f).Text(text => text.Span($"Invoice {invoice.Number}").Bold());
                        row.RelativeItem(4f).Text(text =>
                        {
                            text.AlignRight();
                            text.Span(invoice.CustomerName);
                        });
                    });

                    column.Item().Height(12f);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            Cell(header.Cell(), "Description", isHeader: true, right: false);
                            Cell(header.Cell(), "Qty", isHeader: true, right: true);
                            Cell(header.Cell(), "Total", isHeader: true, right: true);
                        });

                        foreach (var line in invoice.Lines)
                        {
                            Cell(table.Cell(), line.Description, isHeader: false, right: false);
                            Cell(table.Cell(), Number(line.Quantity), isHeader: false, right: true);
                            Cell(table.Cell(), Money(line.Total), isHeader: false, right: true);
                        }
                    });

                    column.Item().Height(8f);

                    column.Item().Text(text =>
                    {
                        text.AlignRight();
                        text.Span($"Total due: {Money(invoice.Total)}").Bold();
                    });
                });

                page.Footer().Text("Thank you for your business.");
            }))
            .WithMetadata(new QuestMetadata
            {
                Title = $"Invoice {invoice.Number}",
                Author = "ACME Corp",
                Keywords = $"invoice, {invoice.Number}",
            })
            .GeneratePdf();

    public static byte[] Contract(ContractDto contract) =>
        QuestDocument.Create(container =>
            {
                // Cover page: no running furniture.
                container.Page(page =>
                {
                    ConfigurePage(page, 595.28f, 841.89f, 72f);
                    page.Content().Column(column => ComposeCover(column, contract));
                });

                // Body: QuestPDF paginates and resolves the page-number footer natively.
                container.Page(page =>
                {
                    ConfigurePage(page, 595.28f, 841.89f, 64f);

                    page.Header().Text(text =>
                        Mute(text.Span($"{contract.Reference} — {contract.Title}")));

                    page.Footer().Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });

                    page.Content().Column(column =>
                    {
                        for (var i = 0; i < contract.Articles.Count; i++)
                        {
                            ComposeArticle(column, i + 1, contract.Articles[i]);
                        }

                        ComposeSignatures(column, contract.Signatories);
                    });
                });
            })
            .WithMetadata(new QuestMetadata
            {
                Title = contract.Title,
                Subject = contract.Reference,
                Author = contract.Lender.Name,
                Creator = "FluentPdf",
                Keywords = $"facility agreement, {contract.Reference}",
            })
            .GeneratePdf();

    private static void ComposeCover(ColumnDescriptor column, ContractDto contract)
    {
        column.Item().Height(120f);
        column.Item().Text(text => text.Span(contract.Title).FontSize(22f).Bold());
        column.Item().Height(8f);
        column.Item().Text(text => Mute(text.Span(contract.Reference)));
        column.Item().Text(text => Mute(text.Span(
            $"Dated {contract.Date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)}")));
        column.Item().Height(28f);
        column.Item().Text(text => text.Span("Between").Bold());
        column.Item().Text($"{contract.Lender.Name} (the \"{contract.Lender.Role}\")");
        column.Item().Text(contract.Lender.Address);
        column.Item().Height(8f);
        column.Item().Text(text => text.Span("and").Bold());
        column.Item().Text($"{contract.Borrower.Name} (the \"{contract.Borrower.Role}\")");
        column.Item().Text(contract.Borrower.Address);
        column.Item().Height(28f);
        column.Item().Text(text => text.Span("Recitals").Bold());
        column.Item().Height(4f);

        var index = 0;
        foreach (var recital in contract.Recitals)
        {
            index++;
            var letter = (char)('A' + index - 1);
            column.Item().Text(text =>
            {
                text.Span($"({letter})  ").Bold();
                text.Span(recital);
            });
            column.Item().Height(6f);
        }
    }

    private static void ComposeArticle(ColumnDescriptor column, int number, ContractArticle article)
    {
        column.Item().Text(text => text.Span($"Article {number}. {article.Title}").FontSize(13f).Bold());
        column.Item().Height(6f);

        for (var i = 0; i < article.Clauses.Count; i++)
        {
            var clauseNumber = $"{number}.{i + 1}";
            column.Item().Text(text =>
            {
                text.Span($"{clauseNumber}  ").Bold();
                text.Span(article.Clauses[i]);
            });
            column.Item().Height(6f);
        }

        column.Item().Height(10f);
    }

    private static void ComposeSignatures(ColumnDescriptor column, IReadOnlyList<Party> signatories)
    {
        column.Item().Height(16f);
        column.Item().Text(text => text.Span("Execution").FontSize(13f).Bold());
        column.Item().Height(6f);
        column.Item().Text("IN WITNESS WHEREOF the parties have executed this Agreement as a deed "
            + "and have caused it to be delivered on the date stated at the beginning of "
            + "this Agreement.");
        column.Item().Height(16f);

        foreach (var party in signatories)
        {
            column.Item().Text(text => text.Span(party.Role).Bold());
            column.Item().Text(party.Name);
            column.Item().Height(6f);
            column.Item().Text("Signature: ______________________________");
            column.Item().Text("Name:");
            column.Item().Text("Title:");
            column.Item().Height(18f);
        }
    }

    private static void ConfigurePage(PageDescriptor page, float width, float height, float margin)
    {
        page.Size(width, height, Unit.Point);
        page.MarginTop(margin, Unit.Point);
        page.MarginRight(margin, Unit.Point);
        page.MarginBottom(margin, Unit.Point);
        page.MarginLeft(margin, Unit.Point);
        page.DefaultTextStyle(text => text.FontFamily(EmbeddedFonts.Family).FontSize(11f));
    }

    private static void Cell(IContainer container, string text, bool isHeader, bool right)
    {
        var box = container.Border(RenderingTheme.BorderWidth).BorderColor(Hex(RenderingTheme.BorderColor));

        if (isHeader)
        {
            box = box.Background(Hex(RenderingTheme.HeaderBackground));
        }

        box = box.Padding(RenderingTheme.CellPadding);

        if (right)
        {
            box = box.AlignRight();
        }

        box.Text(text);
    }

    private static TextSpanDescriptor Mute(TextSpanDescriptor span) => span.FontSize(9f).FontColor(Muted);

    private static string Hex((byte R, byte G, byte B) color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
