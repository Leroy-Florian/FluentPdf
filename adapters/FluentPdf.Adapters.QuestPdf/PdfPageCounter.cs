using System.Text;
using System.Text.RegularExpressions;

namespace FluentPdf.Adapters.QuestPdf;

/// <summary>
/// Counts the pages of a PDF produced by QuestPDF without any third-party dependency. QuestPDF's
/// Skia backend writes classic, uncompressed objects (no object streams), so counting the page
/// objects (<c>/Type /Page</c>, excluding the <c>/Type /Pages</c> tree node) is reliable — and
/// keeps the adapter free of a PDF-reading library.
/// </summary>
internal static partial class PdfPageCounter
{
    public static int Count(byte[] pdf)
    {
        // Latin1 maps each byte to one char, so PDF tokens (ASCII) survive intact.
        var text = Encoding.Latin1.GetString(pdf);
        var matches = PageObject().Matches(text).Count;
        return matches > 0 ? matches : 1;
    }

    // "/Type /Page" with optional whitespace, not followed by a letter (so "/Type /Pages" is excluded).
    [GeneratedRegex(@"/Type\s*/Page(?![A-Za-z])")]
    private static partial Regex PageObject();
}
