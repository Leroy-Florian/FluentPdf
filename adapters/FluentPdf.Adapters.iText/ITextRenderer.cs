using FluentPdf.Application.Rendering;
using FluentPdf.Domain;
using FluentPdf.Kernel;
using iText.Kernel.Pdf;
using iText.Layout;
using DomainDocument = FluentPdf.Domain.PdfDocument;
using ITextPdfDocument = iText.Kernel.Pdf.PdfDocument;

namespace FluentPdf.Adapters.IText;

/// <summary>
/// A real <see cref="IPdfRenderer"/> backed by iText 7. It maps the agnostic model onto
/// iText's layout elements and lets iText paginate; running headers/footers and "Page X of Y"
/// fields are drawn in a second pass once the final page count is known. Charts are only
/// rendered when an <see cref="IChartRenderer"/> is supplied (e.g. the optional
/// FluentPdf.Charting.Skia package); otherwise the adapter declares the
/// <see cref="PdfFeature.Chart"/> capability unsupported.
/// </summary>
public sealed class ITextRenderer : IPdfRenderer
{
    private readonly IChartRenderer? _charts;

    /// <summary>
    /// Creates the renderer. Pass an <see cref="IChartRenderer"/> to enable chart rendering
    /// (and advertise the <see cref="PdfFeature.Chart"/> capability); omit it to stay
    /// dependency-light.
    /// </summary>
    public ITextRenderer(IChartRenderer? chartRenderer = null)
    {
        _charts = chartRenderer;

        var supported = chartRenderer is null
            ? RendererCapabilities.Everything & ~PdfFeature.Chart
            : RendererCapabilities.Everything;

        Descriptor = new RendererDescriptor("iText", new RendererCapabilities(supported));
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
            using var stream = new MemoryStream();
            var pageCount = Write(document, stream);

            return RenderedPdf.Create(stream.ToArray(), pageCount);
        }
        catch (Exception exception)
        {
            return Error.Validation("iText.RenderFailed", exception.Message);
        }
    }

    private int Write(DomainDocument document, Stream stream)
    {
        var pdf = new ITextPdfDocument(new PdfWriter(stream));
        ApplyMetadata(pdf, document.Metadata);

        var composer = new ITextComposer(_charts);

        Document? layout = null;
        var ranges = new List<SectionRange>();
        var previousEnd = 0;

        foreach (var section in document.Sections)
        {
            var pageSize = ITextComposer.PageSizeOf(section);

            if (layout is null)
            {
                // immediateFlush: false keeps pages in memory so furniture can be drawn on
                // them in a second pass once the final page count is known.
                layout = new Document(pdf, pageSize, immediateFlush: false);
            }
            else
            {
                layout.Add(new iText.Layout.Element.AreaBreak(pageSize));
            }

            layout.SetMargins(
                (float)section.Margins.Top,
                (float)section.Margins.Right,
                (float)section.Margins.Bottom,
                (float)section.Margins.Left);

            composer.ComposeBody(layout, section.Blocks);

            // Pages are laid out (though not flushed) during Add, so this is the section's last
            // page; the section begins on the page after the previous section ended.
            var end = pdf.GetNumberOfPages();
            ranges.Add(new SectionRange(section, previousEnd + 1, end));
            previousEnd = end;
        }

        var total = pdf.GetNumberOfPages();
        foreach (var range in ranges)
        {
            for (var page = range.Start; page <= range.End; page++)
            {
                composer.DrawFurniture(pdf, page, range.Section, total);
            }
        }

        layout!.Close();
        return total;
    }

    private static void ApplyMetadata(ITextPdfDocument pdf, DocumentMetadata metadata)
    {
        var info = pdf.GetDocumentInfo();

        if (!string.IsNullOrWhiteSpace(metadata.Title))
        {
            info.SetTitle(metadata.Title);
        }

        if (!string.IsNullOrWhiteSpace(metadata.Author))
        {
            info.SetAuthor(metadata.Author);
        }

        if (!string.IsNullOrWhiteSpace(metadata.Subject))
        {
            info.SetSubject(metadata.Subject);
        }

        if (!string.IsNullOrWhiteSpace(metadata.Creator))
        {
            info.SetCreator(metadata.Creator);
        }

        if (metadata.Keywords.Count > 0)
        {
            info.SetKeywords(string.Join(", ", metadata.Keywords));
        }
    }

    private readonly record struct SectionRange(Section Section, int Start, int End);
}
