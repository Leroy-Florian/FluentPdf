using FluentPdf.Adapters.Shared;
using FluentPdf.Application.Rendering;
using FluentPdf.Domain;
using FluentPdf.Kernel;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using DomainDocument = FluentPdf.Domain.PdfDocument;
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
    static QuestPdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // Register the embedded Liberation Sans so text metrics match the iText adapter.
        FontManager.RegisterFont(new MemoryStream(EmbeddedFonts.Regular));
        FontManager.RegisterFont(new MemoryStream(EmbeddedFonts.Bold));
        FontManager.RegisterFont(new MemoryStream(EmbeddedFonts.Italic));
        FontManager.RegisterFont(new MemoryStream(EmbeddedFonts.BoldItalic));
    }

    /// <inheritdoc />
    public RendererDescriptor Descriptor { get; } = new(
        "QuestPDF",
        RendererCapabilities.Full);

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

        return RenderedPdf.Create(bytes, PdfPageCounter.Count(bytes));
    }
}
