using FluentPdf.Application.Rendering;
using FluentPdf.Domain;
using FluentPdf.Kernel;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using DomainDocument = FluentPdf.Domain.PdfDocument;
using PdfSharpDocument = PdfSharp.Pdf.PdfDocument;

namespace FluentPdf.Adapters.PdfSharp;

/// <summary>
/// A real <see cref="IPdfRenderer"/> backed by the PDFsharp/MigraDoc family (MIT-licensed and
/// fully managed — no native dependencies). It maps the agnostic model onto MigraDoc's
/// document-flow model and lets MigraDoc paginate and resolve running headers/footers and
/// "Page X of Y" fields. Charts are only rendered when an <see cref="IChartRenderer"/> is
/// supplied (e.g. the optional FluentPdf.Charting.Skia package); otherwise the adapter declares
/// the <see cref="PdfFeature.Chart"/> capability unsupported — so the core stays free of SkiaSharp.
/// </summary>
public sealed class PdfSharpRenderer : IPdfRenderer
{
    static PdfSharpRenderer()
    {
        // PDFsharp resolves every typeface through a font resolver rather than the host's
        // installed fonts; register the embedded Liberation Sans once for the process.
        GlobalFontSettings.FontResolver ??= EmbeddedFontResolver.Instance;
    }

    private readonly IChartRenderer? _charts;

    /// <summary>
    /// Creates the renderer. Pass an <see cref="IChartRenderer"/> to enable chart rendering
    /// (and advertise the <see cref="PdfFeature.Chart"/> capability); omit it to stay
    /// dependency-light.
    /// </summary>
    public PdfSharpRenderer(IChartRenderer? chartRenderer = null)
    {
        _charts = chartRenderer;

        var supported = chartRenderer is null
            ? RendererCapabilities.Everything & ~PdfFeature.Chart
            : RendererCapabilities.Everything;

        Descriptor = new RendererDescriptor("PDFsharp", new RendererCapabilities(supported));
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

        try
        {
            var migraDoc = new PdfSharpComposer(_charts).Compose(document);

            var renderer = new PdfDocumentRenderer { Document = migraDoc };
            renderer.RenderDocument();

            var pdf = renderer.PdfDocument;

            // MigraDoc copies its Document.Info to the PDF, but has no Creator field of its own.
            if (!string.IsNullOrWhiteSpace(document.Metadata.Creator))
            {
                pdf.Info.Creator = document.Metadata.Creator;
            }

            // Read the page count before saving: saving finalises the document, after which the
            // renderer's PdfDocument can no longer be touched.
            var pageCount = pdf.PageCount > 0 ? pdf.PageCount : 1;

            using var stream = new MemoryStream();
            pdf.Save(stream, closeStream: false);

            return RenderedPdf.Create(stream.ToArray(), pageCount);
        }
        catch (Exception exception)
        {
            return Error.Validation("PdfSharp.RenderFailed", exception.Message);
        }
    }

    private static int PageCount(PdfSharpDocument pdf) => pdf.PageCount > 0 ? pdf.PageCount : 1;
}
