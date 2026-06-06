using FluentPdf.Kernel;

namespace FluentPdf.Kernel.UnitTests;

public sealed class ResultTests
{
    private static readonly Error SampleError = Error.Validation("Test.Code", "message");

    [Fact]
    public void Success_is_successful_and_carries_no_error()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_is_not_successful_and_carries_the_error()
    {
        var result = Result.Failure(SampleError);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SampleError);
    }

    [Fact]
    public void Success_with_value_exposes_the_value()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Accessing_the_value_of_a_failure_throws()
    {
        var result = Result.Failure<int>(SampleError);

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Implicit_conversion_from_value_produces_success()
    {
        Result<int> result = 7;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(7);
    }

    [Fact]
    public void Implicit_conversion_from_error_produces_failure()
    {
        Result<int> result = SampleError;

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SampleError);
    }

    [Fact]
    public void A_successful_result_cannot_carry_an_error()
    {
        var illegal = () => new IllegalResultProbe(true, SampleError);

        illegal.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void A_failed_result_must_carry_an_error()
    {
        var illegal = () => new IllegalResultProbe(false, Error.None);

        illegal.Should().Throw<InvalidOperationException>();
    }

    // Exercises the protected Result constructor guards via a minimal subclass.
    private sealed class IllegalResultProbe : Result
    {
        public IllegalResultProbe(bool isSuccess, Error error)
            : base(isSuccess, error)
        {
        }
    }
}
