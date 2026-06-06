using FluentPdf.Application.Building;
using FluentPdf.Domain;
using FluentPdf.Kernel;

namespace FluentPdf.Samples.Invoice;

/// <summary>
/// A reusable template that assembles a complete invoice document from an
/// <see cref="InvoiceDto"/>, composing two reusable components inside a single section with
/// a running header and footer. The same instance renders any number of invoices.
/// </summary>
public sealed class InvoiceTemplate : IDocumentTemplate<InvoiceDto>
{
    private readonly InvoiceHeaderComponent _header = new();
    private readonly InvoiceLinesComponent _lines = new();

    public Result<PdfDocument> Build(InvoiceDto invoice) =>
        PdfDocumentBuilder.Create()
            .Metadata(meta => meta
                .Title($"Invoice {invoice.Number}")
                .Author("ACME Corp")
                .Keywords("invoice", invoice.Number))
            .Section(section => section
                .Margins(48d)
                .Header(header => header.Paragraph("ACME Corp — 123 Example Street"))
                .Footer(footer => footer.Paragraph("Thank you for your business."))
                .Component(_header, invoice)
                .Component(_lines, invoice))
            .Build();
}
