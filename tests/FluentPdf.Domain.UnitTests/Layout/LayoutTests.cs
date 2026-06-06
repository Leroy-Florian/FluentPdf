using FluentPdf.Domain.Layout;

namespace FluentPdf.Domain.UnitTests.Layout;

public sealed class PageSizeTests
{
    [Fact]
    public void Presets_are_portrait()
    {
        PageSize.A4.Orientation.Should().Be(PageOrientation.Portrait);
        PageSize.Letter.Orientation.Should().Be(PageOrientation.Portrait);
    }

    [Fact]
    public void Create_builds_a_custom_size()
    {
        var result = PageSize.Create(100d, 200d);

        result.IsSuccess.Should().BeTrue();
        result.Value.Width.Should().Be(100d);
        result.Value.Height.Should().Be(200d);
    }

    [Theory]
    [InlineData(0d, 100d)]
    [InlineData(100d, 0d)]
    [InlineData(-1d, 100d)]
    [InlineData(100d, -1d)]
    public void Create_rejects_non_positive_dimensions(double width, double height)
    {
        var result = PageSize.Create(width, height);

        result.Error.Should().Be(DomainErrors.PageSize.NonPositiveDimension);
    }

    [Fact]
    public void WithOrientation_landscape_swaps_dimensions()
    {
        var landscape = PageSize.A4.WithOrientation(PageOrientation.Landscape);

        landscape.Width.Should().Be(PageSize.A4.Height);
        landscape.Height.Should().Be(PageSize.A4.Width);
        landscape.Orientation.Should().Be(PageOrientation.Landscape);
    }

    [Fact]
    public void WithOrientation_returns_same_instance_when_already_correct()
    {
        var portrait = PageSize.A4.WithOrientation(PageOrientation.Portrait);

        portrait.Should().BeSameAs(PageSize.A4);
    }

    [Fact]
    public void Sizes_compare_by_value()
    {
        PageSize.Create(10d, 20d).Value.Should().Be(PageSize.Create(10d, 20d).Value);
    }
}

public sealed class MarginsTests
{
    [Fact]
    public void Create_sets_each_side()
    {
        var result = Margins.Create(1d, 2d, 3d, 4d);

        result.IsSuccess.Should().BeTrue();
        result.Value.Top.Should().Be(1d);
        result.Value.Right.Should().Be(2d);
        result.Value.Bottom.Should().Be(3d);
        result.Value.Left.Should().Be(4d);
    }

    [Theory]
    [InlineData(-1d, 0d, 0d, 0d)]
    [InlineData(0d, -1d, 0d, 0d)]
    [InlineData(0d, 0d, -1d, 0d)]
    [InlineData(0d, 0d, 0d, -1d)]
    public void Create_rejects_negative_sides(double top, double right, double bottom, double left)
    {
        var result = Margins.Create(top, right, bottom, left);

        result.Error.Should().Be(DomainErrors.Margins.NegativeValue);
    }

    [Fact]
    public void Uniform_sets_all_sides_equally()
    {
        var result = Margins.Uniform(5d);

        result.Value.Top.Should().Be(5d);
        result.Value.Right.Should().Be(5d);
        result.Value.Bottom.Should().Be(5d);
        result.Value.Left.Should().Be(5d);
    }

    [Fact]
    public void Symmetric_sets_vertical_and_horizontal()
    {
        var result = Margins.Symmetric(vertical: 3d, horizontal: 7d);

        result.Value.Top.Should().Be(3d);
        result.Value.Bottom.Should().Be(3d);
        result.Value.Left.Should().Be(7d);
        result.Value.Right.Should().Be(7d);
    }

    [Fact]
    public void None_is_all_zero()
    {
        Margins.None.Should().Be(Margins.Create(0d, 0d, 0d, 0d).Value);
    }
}
