using FluentPdf.Application.Rendering;
using FluentPdf.Domain;
using FluentPdf.Kernel;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using DomainDocument = FluentPdf.Domain.PdfDocument;
using ITextPdfDocument = iText.Kernel.Pdf.PdfDocument;
using ITextPdfReader = iText.Kernel.Pdf.PdfReader;
using QuestDocument = QuestPDF.Fluent.Document;

namespace FluentPdf.Adapters.QuestPdf;

/// <summary>
/// A real <see cref="IPdfRenderer"/> backed by QuestPDF. It translates the agnostic document
/// model into QuestPDF's fluent layout API and lets QuestPDF do its own (high-quality)
/// pagination — page-number fields map onto QuestPDF's native page counters. Charts are not
/// translated, so the adapter declares the <see cref="PdfFeature.Chart"/> capability
/// unsupported and the rendering use case refuses chart content rather than dropping it.
/// </summary>
public sealed class QuestPdfRenderer : IPdfRenderer
{
    static QuestPdfRenderer() => QuestPDF.Settings.License = LicenseType.Community;

    /// <inheritdoc />
    public RendererDescriptor Descriptor { get; } = new(
        "QuestPDF",
        new RendererCapabilities(RendererCapabilities.Everything & ~PdfFeature.Chart));

    /// <inheritdoc />
    public Result<RenderedPdf> Render(DomainDocument document)
    {
        if (document is null)
        {
            return RenderErrors.NullDocument;
        }

        byte[] bytes;
        try
        {
            bytes = QuestDocument.Create(container => QuestPdfComposer.Compose(container, document))
                .WithMetadata(QuestPdfComposer.Metadata(document.Metadata))
                .GeneratePdf();
        }
        catch (Exception exception)
        {
            return Error.Validation("QuestPdf.RenderFailed", exception.Message);
        }

        return RenderedPdf.Create(bytes, CountPages(bytes));
    }

    private static int CountPages(byte[] bytes)
    {
        using var reader = new ITextPdfReader(new MemoryStream(bytes));
        using var pdf = new ITextPdfDocument(reader);
        return pdf.GetNumberOfPages();
    }
}
