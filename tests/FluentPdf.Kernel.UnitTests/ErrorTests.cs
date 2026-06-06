using FluentPdf.Kernel;

namespace FluentPdf.Kernel.UnitTests;

public sealed class ErrorTests
{
    [Fact]
    public void None_has_empty_code_and_message()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
    }

    [Fact]
    public void Validation_sets_code_and_message()
    {
        var error = Error.Validation("Some.Code", "Some message");

        error.Code.Should().Be("Some.Code");
        error.Message.Should().Be("Some message");
    }

    [Fact]
    public void Errors_compare_by_value()
    {
        var a = Error.Validation("X", "y");
        var b = Error.Validation("X", "y");

        a.Should().Be(b);
    }

    [Fact]
    public void Errors_with_different_codes_are_not_equal()
    {
        Error.Validation("A", "m").Should().NotBe(Error.Validation("B", "m"));
    }
}
