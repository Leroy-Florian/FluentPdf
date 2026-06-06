using FluentPdf.Application.Building;
using FluentPdf.Domain;
using FluentPdf.Domain.Content;

namespace FluentPdf.Conformance;

/// <summary>
/// A library of well-known documents used by the conformance suite. Each is built through
/// the public fluent API, so the same inputs every adapter receives are exercised here.
/// </summary>
public static class CanonicalDocuments
{
    /// <summary>A unique marker string that must survive rendering into the output text.</summary>
    public const string TextMarker = "FluentPdf-conformance-marker-7f3a";

    /// <summary>A minimal document using only the baseline feature set.</summary>
    public static PdfDocument Baseline() =>
        PdfDocumentBuilder.Create()
            .Metadata(m => m.Title("Baseline"))
            .Section(s => s
                .Paragraph(TextMarker)
                .Spacer(10d))
            .Build()
            .Value;

    /// <summary>A document whose body contains <paramref name="marker"/> verbatim.</summary>
    public static PdfDocument WithText(string marker) =>
        PdfDocumentBuilder.Create()
            .Section(s => s.Paragraph(marker))
            .Build()
            .Value;

    /// <summary>A single section containing <paramref name="pageBreaks"/> explicit breaks,
    /// which must yield at least <c>pageBreaks + 1</c> pages.</summary>
    public static PdfDocument WithPageBreaks(int pageBreaks)
    {
        var builder = PdfDocumentBuilder.Create();

        return builder
            .Section(s =>
            {
                s.Paragraph("page 1");

                for (var i = 0; i < pageBreaks; i++)
                {
                    s.PageBreak().Paragraph($"after break {i + 1}");
                }
            })
            .Build()
            .Value;
    }

    /// <summary>A document that exercises every feature the model defines.</summary>
    public static PdfDocument FullFeature() =>
        PdfDocumentBuilder.Create()
            .Metadata(m => m
                .Title("Full feature")
                .Author("FluentPdf")
                .Keywords("pdf", "conformance"))
            .Section(s => s
                .Header(h => h.Paragraph("header"))
                .Footer(f => f.Paragraph("footer"))
                .Paragraph(p => p.Text("normal ").Bold("bold ").Italic("italic"))
                .Row(r => r
                    .Column(6, c => c.Paragraph("left column"))
                    .Column(6, c => c.Paragraph("right column")))
                .UnorderedList(l => l.Item("first").Item("second"))
                .OrderedList(l => l.Item("one").Item("two"))
                .Table(t => t
                    .Columns(2)
                    .HeaderRow(r => r.Cell("Name").Cell("Value"))
                    .Row(r => r.Cell("Alpha").Cell("1")))
                .Image([0x89, 0x50, 0x4E, 0x47], ImageFormat.Png, 64d, 64d)
                .Chart(c => c
                    .Bar()
                    .Title("conformance chart")
                    .Categories("A", "B")
                    .Series("Series 1", 1d, 2d))
                .Spacer(12d)
                .PageBreak()
                .Paragraph("second page"))
            .Build()
            .Value;
}
