namespace FluentPdf.Application.Rendering;

/// <summary>
/// The set of <see cref="PdfFeature"/>s an adapter is able to render. Adapters publish
/// their capabilities so consumers (and the conformance kit) can verify, before rendering,
/// that a document only uses supported features.
/// </summary>
public sealed class RendererCapabilities
{
    /// <summary>The conventional baseline every adapter is expected to support.</summary>
    public const PdfFeature Baseline =
        PdfFeature.Paragraph | PdfFeature.Spacer | PdfFeature.PageBreak | PdfFeature.Metadata;

    /// <summary>Every feature the model defines.</summary>
    public const PdfFeature Everything =
        PdfFeature.Paragraph | PdfFeature.Image | PdfFeature.Table | PdfFeature.List |
        PdfFeature.Spacer | PdfFeature.PageBreak | PdfFeature.Header | PdfFeature.Footer |
        PdfFeature.Metadata | PdfFeature.Grid | PdfFeature.Chart;

    public RendererCapabilities(PdfFeature supported) => Supported = supported;

    /// <summary>The features this adapter supports.</summary>
    public PdfFeature Supported { get; }

    /// <summary>An adapter that supports every feature the model defines.</summary>
    public static RendererCapabilities Full { get; } = new(Everything);

    /// <summary>An adapter that supports only the baseline feature set.</summary>
    public static RendererCapabilities Basic { get; } = new(Baseline);

    /// <summary>Whether every flag in <paramref name="feature"/> is supported.</summary>
    public bool Supports(PdfFeature feature) => (Supported & feature) == feature;

    /// <summary>
    /// Returns the subset of <paramref name="required"/> that this adapter cannot render,
    /// or <see cref="PdfFeature.None"/> when everything required is supported.
    /// </summary>
    public PdfFeature Missing(PdfFeature required) => required & ~Supported;
}
