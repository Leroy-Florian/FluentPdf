using FluentPdf.Domain;
using FluentPdf.Domain.Content;
using FluentPdf.Domain.Styling;

namespace FluentPdf.Domain.UnitTests.Content;

public sealed class PageNumberFieldTests
{
    [Fact]
    public void Resolves_both_tokens()
    {
        var field = PageNumberField.Create("Page {page} of {pages}", HorizontalAlignment.Center).Value;

        field.Resolve(3, 40).Should().Be("Page 3 of 40");
        field.Alignment.Should().Be(HorizontalAlignment.Center);
    }

    [Fact]
    public void Reports_whether_it_needs_the_total()
    {
        PageNumberField.Create("Page {page} of {pages}").Value.UsesTotalPages.Should().BeTrue();
        PageNumberField.Create("Page {page}").Value.UsesTotalPages.Should().BeFalse();
    }

    [Fact]
    public void An_empty_format_fails()
    {
        PageNumberField.Create("").Error.Should().Be(DomainErrors.PageNumber.EmptyFormat);
    }
}
