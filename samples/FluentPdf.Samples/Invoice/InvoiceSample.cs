using FluentPdf.Application.Rendering;
using FluentPdf.Application.UseCases;
using FluentPdf.Infrastructure.Rendering;
using FluentPdf.Kernel;

namespace FluentPdf.Samples.Invoice;

/// <summary>
/// End-to-end usage: build a DTO, render it through a template, then through a renderer
/// adapter. Swapping <see cref="InMemoryPdfRenderer"/> for an iText/QuestPDF adapter is the
/// only change needed to produce a real PDF — the template and components stay identical.
/// </summary>
public static class InvoiceSample
{
    public static Result<RenderedPdf> Render(IPdfRenderer renderer)
    {
        var document = new InvoiceTemplate().Build(SampleData());

        if (document.IsFailure)
        {
            return document.Error;
        }

        // The use case verifies the adapter supports every feature the invoice uses,
        // then renders it.
        return new RenderDocumentUseCase(renderer).Execute(document.Value);
    }

    /// <summary>The sample invoice DTO, shared by the sample and the benchmarks.</summary>
    public static InvoiceDto SampleData() => new(
        Number: "INV-001",
        CustomerName: "Jane Doe",
        Lines:
        [
            new InvoiceLine("Widget", 2, 9.99m),
            new InvoiceLine("Gadget", 1, 19.99m),
        ]);

    /// <summary>Renders the sample invoice with the built-in in-memory adapter.</summary>
    public static Result<RenderedPdf> RenderWithInMemoryAdapter() =>
        Render(new InMemoryPdfRenderer());
}
