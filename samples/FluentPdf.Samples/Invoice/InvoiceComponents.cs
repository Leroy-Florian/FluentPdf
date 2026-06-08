using System.Globalization;
using FluentPdf.Application.Building;
using FluentPdf.Domain.Content;
using FluentPdf.Domain.Styling;
using FluentPdf.Kernel;

namespace FluentPdf.Samples.Invoice;

/// <summary>
/// A reusable header block: the invoice number on the left, the customer on the right,
/// laid out on the 12-unit grid. Define it once, reuse it in any document.
/// </summary>
public sealed class InvoiceHeaderComponent : IBlockComponent<InvoiceDto>
{
    public Result<IReadOnlyList<IBlock>> Build(InvoiceDto invoice) =>
        BlockComposer.Compose(blocks => blocks
            .Row(row => row
                .Column(8, col => col.Paragraph(p => p.Bold($"Invoice {invoice.Number}")))
                .Column(4, col => col.Paragraph(
                    invoice.CustomerName,
                    alignment: HorizontalAlignment.Right)))
            .Spacer(12d));
}

/// <summary>
/// A reusable lines block: a table of billed lines followed by the total. Driven entirely
/// by the DTO, so the same component renders any invoice.
/// </summary>
public sealed class InvoiceLinesComponent : IBlockComponent<InvoiceDto>
{
    public Result<IReadOnlyList<IBlock>> Build(InvoiceDto invoice) =>
        BlockComposer.Compose(blocks => blocks
            .Table(table => table
                .Columns(3)
                .HeaderRow(row => row
                    .Cell("Description")
                    .Cell("Qty", HorizontalAlignment.Right)
                    .Cell("Total", HorizontalAlignment.Right))
                .Rows(invoice.Lines, (row, line) => row
                    .Cell(line.Description)
                    .Cell(Format(line.Quantity), HorizontalAlignment.Right)
                    .Cell(Format(line.Total), HorizontalAlignment.Right)))
            .Spacer(8d)
            .Paragraph(p => p
                .Align(HorizontalAlignment.Right)
                .Bold($"Total due: {Format(invoice.Total)}")));

    private static string Format(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Format(int value) =>
        value.ToString(CultureInfo.InvariantCulture);
}
