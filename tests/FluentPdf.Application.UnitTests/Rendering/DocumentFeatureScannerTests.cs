using FluentPdf.Application.Building;
using FluentPdf.Application.Rendering;
using FluentPdf.Domain.Content;

namespace FluentPdf.Application.UnitTests.Rendering;

public sealed class DocumentFeatureScannerTests
{
    [Fact]
    public void Scanning_null_returns_none()
    {
        DocumentFeatureScanner.Scan(null!).Should().Be(PdfFeature.None);
    }

    [Fact]
    public void Detects_baseline_features()
    {
        var document = PdfDocumentBuilder.Create()
            .Metadata(m => m.Title("t"))
            .Section(s => s.Paragraph("hi").Spacer(1d).PageBreak())
            .Build()
            .Value;

        var features = DocumentFeatureScanner.Scan(document);

        features.Should().HaveFlag(PdfFeature.Metadata);
        features.Should().HaveFlag(PdfFeature.Paragraph);
        features.Should().HaveFlag(PdfFeature.Spacer);
        features.Should().HaveFlag(PdfFeature.PageBreak);
    }

    [Fact]
    public void Detects_header_and_footer()
    {
        var document = PdfDocumentBuilder.Create()
            .Section(s => s
                .Header(h => h.Paragraph("h"))
                .Footer(f => f.Paragraph("f"))
                .Paragraph("body"))
            .Build()
            .Value;

        var features = DocumentFeatureScanner.Scan(document);

        features.Should().HaveFlag(PdfFeature.Header);
        features.Should().HaveFlag(PdfFeature.Footer);
    }

    [Fact]
    public void Detects_nested_features_in_grid_list_and_table()
    {
        var document = PdfDocumentBuilder.Create()
            .Section(s => s
                .Row(r => r.Column(12, c => c.Image([1], ImageFormat.Png, 1d, 1d)))
                .UnorderedList(l => l.Item(i => i.Spacer(1d).Paragraph("x")))
                .Table(t => t.Columns(1).Row(r => r.Cell(c => c.Image([1], ImageFormat.Png, 1d, 1d)))))
            .Build()
            .Value;

        var features = DocumentFeatureScanner.Scan(document);

        features.Should().HaveFlag(PdfFeature.Grid);
        features.Should().HaveFlag(PdfFeature.List);
        features.Should().HaveFlag(PdfFeature.Table);
        features.Should().HaveFlag(PdfFeature.Image);
    }
}
