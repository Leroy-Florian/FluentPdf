using FluentPdf.Adapters.Shared;
using PdfSharp.Fonts;

namespace FluentPdf.Adapters.PdfSharp;

/// <summary>
/// Serves the embedded Liberation Sans (regular + bold) to PDFsharp/MigraDoc. The cross-platform
/// PDFsharp build resolves every typeface through an <see cref="IFontResolver"/> rather than the
/// host's system fonts, so embedding the same font the other adapters use keeps glyph metrics
/// consistent and the adapter free of any machine-installed font. Italics are synthesised
/// (faux-italic) because no italic weight is shipped — matching the QuestPDF and iText adapters.
/// </summary>
internal sealed class EmbeddedFontResolver : IFontResolver
{
    private const string RegularFace = "LiberationSans";
    private const string BoldFace = "LiberationSans#b";

    public static EmbeddedFontResolver Instance { get; } = new();

    public byte[] GetFont(string faceName) =>
        faceName == BoldFace ? EmbeddedFonts.Bold : EmbeddedFonts.Regular;

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
        // Bold is satisfied by the dedicated weight; italic is simulated (skew) by PDFsharp.
        new(isBold ? BoldFace : RegularFace, mustSimulateBold: false, mustSimulateItalic: isItalic);
}
