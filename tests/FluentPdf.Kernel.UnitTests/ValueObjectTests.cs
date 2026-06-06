using FluentPdf.Kernel;

namespace FluentPdf.Kernel.UnitTests;

public sealed class ValueObjectTests
{
    [Fact]
    public void Value_objects_with_equal_components_are_equal()
    {
        var a = new Money(10, "EUR");
        var b = new Money(10, "EUR");

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Value_objects_with_different_components_are_not_equal()
    {
        var a = new Money(10, "EUR");
        var b = new Money(10, "USD");

        a.Equals(b).Should().BeFalse();
        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void A_value_object_is_not_equal_to_null()
    {
        var a = new Money(10, "EUR");

        a.Equals(null).Should().BeFalse();
        (a == null).Should().BeFalse();
        (null == a).Should().BeFalse();
    }

    [Fact]
    public void Two_null_references_are_equal()
    {
        Money? left = null;
        Money? right = null;

        (left == right).Should().BeTrue();
    }

    [Fact]
    public void Value_objects_of_different_types_are_not_equal()
    {
        var money = new Money(1, "EUR");
        var weight = new Weight(1);

        money.Equals(weight).Should().BeFalse();
    }

    [Fact]
    public void Equals_object_overload_handles_non_value_objects()
    {
        var money = new Money(1, "EUR");

        money.Equals("not a value object").Should().BeFalse();
    }

    private sealed class Money(decimal amount, string currency) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return amount;
            yield return currency;
        }
    }

    private sealed class Weight(decimal kilograms) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return kilograms;
        }
    }
}
