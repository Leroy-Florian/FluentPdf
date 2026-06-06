using FluentPdf.Application.Rendering;
using FluentPdf.Domain;
using FluentPdf.Kernel;

namespace FluentPdf.Application.UseCases;

/// <summary>
/// Renders a document through a chosen adapter, first verifying that the adapter supports
/// every feature the document uses. Unsupported content fails fast with a named error
/// instead of being silently dropped or misrendered by the underlying library.
/// </summary>
public sealed class RenderDocumentUseCase(IPdfRenderer renderer)
    : IUseCase<PdfDocument, RenderedPdf>
{
    private readonly IPdfRenderer _renderer = renderer
        ?? throw new ArgumentNullException(nameof(renderer));

    /// <inheritdoc />
    public Result<RenderedPdf> Execute(PdfDocument document)
    {
        if (document is null)
        {
            return RenderErrors.NullDocument;
        }

        var required = DocumentFeatureScanner.Scan(document);
        var missing = _renderer.Descriptor.Capabilities.Missing(required);

        if (missing != PdfFeature.None)
        {
            return RenderErrors.UnsupportedFeatures(_renderer.Descriptor.Name, missing);
        }

        return _renderer.Render(document);
    }
}
