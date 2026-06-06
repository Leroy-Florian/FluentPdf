using FluentPdf.Domain.Styling;

namespace FluentPdf.Domain.UnitTests.Styling;

public sealed class ColorTests
{
    [Fact]
    public void FromRgb_creates_an_opaque_colour()
    {
        var result = Color.FromRgb(10, 20, 30);

        result.IsSuccess.Should().BeTrue();
        result.Value.Red.Should().Be(10);
        result.Value.Green.Should().Be(20);
        result.Value.Blue.Should().Be(30);
        result.Value.Alpha.Should().Be(1d);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(256, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 256, 0)]
    [InlineData(0, 0, -1)]
    [InlineData(0, 0, 256)]
    public void FromRgb_rejects_components_outside_byte_range(int red, int green, int blue)
    {
        var result = Color.FromRgb(red, green, blue);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Color.ComponentOutOfRange);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void FromRgba_rejects_alpha_outside_zero_to_one(double alpha)
    {
        var result = Color.FromRgba(0, 0, 0, alpha);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Color.AlphaOutOfRange);
    }

    [Fact]
    public void FromHex_parses_six_digit_form()
    {
        var result = Color.FromHex("#0A141E");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Color.FromRgb(10, 20, 30).Value);
    }

    [Fact]
    public void FromHex_parses_shorthand_three_digit_form()
    {
        var result = Color.FromHex("#abc");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Color.FromRgb(0xAA, 0xBB, 0xCC).Value);
    }

    [Fact]
    public void FromHex_parses_eight_digit_form_with_alpha()
    {
        var result = Color.FromHex("FF000080");

        result.IsSuccess.Should().BeTrue();
        result.Value.Red.Should().Be(255);
        result.Value.Alpha.Should().BeApproximately(128d / 255d, 1e-9);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("#12")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    public void FromHex_rejects_invalid_input(string hex)
    {
        var result = Color.FromHex(hex);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Color.InvalidHex);
    }

    [Fact]
    public void Black_and_white_are_defined()
    {
        Color.Black.Should().Be(Color.FromRgb(0, 0, 0).Value);
        Color.White.Should().Be(Color.FromRgb(255, 255, 255).Value);
    }
}
