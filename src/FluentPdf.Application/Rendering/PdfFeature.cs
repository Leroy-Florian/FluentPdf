namespace FluentPdf.Application.Rendering;

/// <summary>
/// The distinct content features a document may use. An adapter declares which of these
/// its underlying PDF library can render via <see cref="RendererCapabilities"/>; the set a
/// document actually uses is computed by <see cref="DocumentFeatureScanner"/>. Comparing
/// the two turns "this library does not support tables" from a silent divergence into an
/// explicit, testable failure.
/// </summary>
[Flags]
public enum PdfFeature
{
    None = 0,
    Paragraph = 1 << 0,
    Image = 1 << 1,
    Table = 1 << 2,
    List = 1 << 3,
    Spacer = 1 << 4,
    PageBreak = 1 << 5,
    Header = 1 << 6,
    Footer = 1 << 7,
    Metadata = 1 << 8,
    Grid = 1 << 9,
    Chart = 1 << 10,
}
