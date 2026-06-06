using FluentPdf.Kernel;

namespace FluentPdf.Application.Rendering;

/// <summary>Errors produced by the rendering pipeline and the renderer port.</summary>
public static class RenderErrors
{
    public static readonly Error NullDocument = Error.Validation(
        "Render.NullDocument",
        "The document to render must not be null.");

    public static readonly Error EmptyOutput = Error.Validation(
        "Render.EmptyOutput",
        "The renderer produced no output.");

    public static readonly Error NonPositivePageCount = Error.Validation(
        "Render.NonPositivePageCount",
        "A rendered document must contain at least one page.");

    public static readonly Error MissingContentType = Error.Validation(
        "Render.MissingContentType",
        "A rendered document must declare a content type.");

    /// <summary>
    /// Builds the error returned when a document uses features the selected adapter cannot
    /// render. The unsupported features are named so the failure is actionable.
    /// </summary>
    public static Error UnsupportedFeatures(string rendererName, PdfFeature missing) =>
        Error.Validation(
            "Render.UnsupportedFeatures",
            $"The '{rendererName}' renderer cannot render the following feature(s): {missing}.");
}
