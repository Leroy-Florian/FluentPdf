using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace FluentPdf.Adapters.IText.ConformanceTests;

/// <summary>Extracts the visible text from rendered PDF bytes using iText, for assertions.</summary>
internal static class PdfText
{
    public static string Extract(IReadOnlyList<byte> content)
    {
        using var reader = new PdfReader(new MemoryStream([.. content]));
        using var pdf = new PdfDocument(reader);

        var text = new StringBuilder();
        for (var page = 1; page <= pdf.GetNumberOfPages(); page++)
        {
            text.AppendLine(PdfTextExtractor.GetTextFromPage(pdf.GetPage(page)));
        }

        return text.ToString();
    }
}
