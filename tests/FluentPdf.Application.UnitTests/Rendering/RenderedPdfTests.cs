using FluentPdf.Application.Rendering;

namespace FluentPdf.Application.UnitTests.Rendering;

public sealed class RenderedPdfTests
{
    [Fact]
    public void Create_defaults_to_the_pdf_content_type()
    {
        var result = RenderedPdf.Create([1, 2, 3], pageCount: 2);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be(RenderedPdf.PdfContentType);
        result.Value.PageCount.Should().Be(2);
        result.Value.Content.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Create_rejects_empty_content()
    {
        RenderedPdf.Create([], pageCount: 1).Error.Should().Be(RenderErrors.EmptyOutput);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_rejects_non_positive_page_count(int pageCount)
    {
        RenderedPdf.Create([1], pageCount).Error.Should().Be(RenderErrors.NonPositivePageCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_rejects_missing_content_type(string contentType)
    {
        RenderedPdf.Create([1], 1, contentType).Error.Should().Be(RenderErrors.MissingContentType);
    }

    [Fact]
    public void ToArray_returns_a_copy()
    {
        var pdf = RenderedPdf.Create([1, 2], 1).Value;

        var copy = pdf.ToArray();
        copy[0] = 9;

        pdf.Content.Should().Equal(1, 2);
    }

    [Fact]
    public void Create_copies_the_input_defensively()
    {
        var data = new byte[] { 1, 2 };

        var pdf = RenderedPdf.Create(data, 1).Value;
        data[0] = 9;

        pdf.Content.Should().Equal(1, 2);
    }
}
