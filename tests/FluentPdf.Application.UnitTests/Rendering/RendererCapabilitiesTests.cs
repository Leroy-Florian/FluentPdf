using FluentPdf.Application.Rendering;

namespace FluentPdf.Application.UnitTests.Rendering;

public sealed class RendererCapabilitiesTests
{
    [Fact]
    public void Full_supports_every_feature()
    {
        RendererCapabilities.Full.Supports(RendererCapabilities.Everything).Should().BeTrue();
    }

    [Fact]
    public void Basic_supports_the_baseline_only()
    {
        RendererCapabilities.Basic.Supports(RendererCapabilities.Baseline).Should().BeTrue();
        RendererCapabilities.Basic.Supports(PdfFeature.Table).Should().BeFalse();
    }

    [Fact]
    public void Supports_requires_all_requested_flags()
    {
        var caps = new RendererCapabilities(PdfFeature.Paragraph | PdfFeature.Table);

        caps.Supports(PdfFeature.Paragraph | PdfFeature.Table).Should().BeTrue();
        caps.Supports(PdfFeature.Paragraph | PdfFeature.Image).Should().BeFalse();
    }

    [Fact]
    public void Missing_returns_unsupported_flags_only()
    {
        var caps = new RendererCapabilities(PdfFeature.Paragraph);

        var missing = caps.Missing(PdfFeature.Paragraph | PdfFeature.Table | PdfFeature.Image);

        missing.Should().Be(PdfFeature.Table | PdfFeature.Image);
    }

    [Fact]
    public void Missing_is_none_when_everything_is_supported()
    {
        RendererCapabilities.Full.Missing(RendererCapabilities.Everything)
            .Should().Be(PdfFeature.None);
    }
}
