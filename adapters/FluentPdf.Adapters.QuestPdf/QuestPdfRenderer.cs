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
/// model into QuestPDF's fluent layout API and lets QuestPDF do its own pagination. Charts are
/// only rendered when an <see cref="IChartRenderer"/> is supplied (e.g. the optional
/// FluentPdf.Charting.Skia package); otherwise the adapter declares the
/// <see cref="PdfFeature.Chart"/> capability unsupported — so the core stays free of SkiaSharp.
/// </summary>
public sealed class QuestPdfRenderer : IPdfRenderer
{
    static QuestPdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // Register the embedded Liberation Sans (regular + bold) so text metrics match the
        // iText adapter; italics are synthesised by QuestPDF when requested.
        FontManager.RegisterFont(new MemoryStream(EmbeddedFonts.Regular));
        FontManager.RegisterFont(new MemoryStream(EmbeddedFonts.Bold));
    }

    private readonly IChartRenderer? _charts;

    /// <summary>
    /// Creates the renderer. Pass an <see cref="IChartRenderer"/> to enable chart rendering
    /// (and advertise the <see cref="PdfFeature.Chart"/> capability); omit it to stay
    /// dependency-light.
    /// </summary>
    public QuestPdfRenderer(IChartRenderer? chartRenderer = null)
    {
        _charts = chartRenderer;

        var supported = chartRenderer is null
            ? RendererCapabilities.Everything & ~PdfFeature.Chart
            : RendererCapabilities.Everything;

        Descriptor = new RendererDescriptor("QuestPDF", new RendererCapabilities(supported));
    }

    /// <inheritdoc />
    public RendererDescriptor Descriptor { get; }

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
            bytes = QuestDocument.Create(container => new QuestPdfComposer(_charts).Compose(container, document))
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
