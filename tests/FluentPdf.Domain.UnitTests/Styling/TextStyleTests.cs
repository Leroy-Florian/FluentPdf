using FluentPdf.Domain.Styling;

namespace FluentPdf.Domain.UnitTests.Styling;

public sealed class TextStyleTests
{
    [Fact]
    public void Default_uses_conventional_values()
    {
        TextStyle.Default.FontFamily.Should().Be(TextStyle.DefaultFontFamily);
        TextStyle.Default.FontSize.Should().Be(TextStyle.DefaultFontSize);
        TextStyle.Default.Color.Should().Be(Color.Black);
        TextStyle.Default.IsBold.Should().BeFalse();
        TextStyle.Default.IsItalic.Should().BeFalse();
        TextStyle.Default.IsUnderlined.Should().BeFalse();
    }

    [Fact]
    public void Create_builds_a_style()
    {
        var result = TextStyle.Create("Arial", 14d, Color.Black, isBold: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.FontFamily.Should().Be("Arial");
        result.Value.FontSize.Should().Be(14d);
        result.Value.IsBold.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_font_family(string fontFamily)
    {
        var result = TextStyle.Create(fontFamily, 12d, Color.Black);

        result.Error.Should().Be(DomainErrors.TextStyle.EmptyFontFamily);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    public void Create_rejects_non_positive_size(double size)
    {
        var result = TextStyle.Create("Arial", size, Color.Black);

        result.Error.Should().Be(DomainErrors.TextStyle.NonPositiveFontSize);
    }

    [Fact]
    public void Create_rejects_null_colour()
    {
        var result = TextStyle.Create("Arial", 12d, null!);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void With_methods_return_modified_copies()
    {
        var style = TextStyle.Default;

        style.WithFontSize(20d).FontSize.Should().Be(20d);
        style.WithColor(Color.White).Color.Should().Be(Color.White);
        style.WithBold().IsBold.Should().BeTrue();
        style.WithItalic().IsItalic.Should().BeTrue();
        style.WithUnderline().IsUnderlined.Should().BeTrue();

        // The original is untouched (immutability).
        style.FontSize.Should().Be(TextStyle.DefaultFontSize);
        style.IsBold.Should().BeFalse();
    }

    [Fact]
    public void Styles_compare_by_value()
    {
        var a = TextStyle.Create("Arial", 12d, Color.Black).Value;
        var b = TextStyle.Create("Arial", 12d, Color.Black).Value;

        a.Should().Be(b);
    }
}
