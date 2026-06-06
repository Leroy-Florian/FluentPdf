using FluentPdf.Domain;
using FluentPdf.Domain.Content;

namespace FluentPdf.Domain.UnitTests.Content;

public sealed class ChartTests
{
    private static ChartSeries Series(string name, params double[] values) =>
        ChartSeries.Create(name, values).Value;

    [Fact]
    public void A_valid_bar_chart_is_created()
    {
        var chart = ChartBlock.Create(
            ChartType.Bar,
            ["Q1", "Q2"],
            [Series("Revenue", 10d, 20d), Series("EBITDA", 3d, 6d)],
            480d,
            240d,
            "Trend").Value;

        chart.Type.Should().Be(ChartType.Bar);
        chart.Title.Should().Be("Trend");
        chart.Categories.Should().Equal("Q1", "Q2");
        chart.Series.Should().HaveCount(2);
        chart.Series[0].Values.Should().Equal(10d, 20d);
    }

    [Fact]
    public void A_blank_title_is_normalised_to_null()
    {
        var chart = ChartBlock.Create(ChartType.Line, ["A"], [Series("s", 1d)], 10d, 10d, "  ").Value;

        chart.Title.Should().BeNull();
    }

    [Fact]
    public void A_chart_requires_categories()
    {
        ChartBlock.Create(ChartType.Bar, [], [Series("s", 1d)], 10d, 10d)
            .Error.Should().Be(DomainErrors.Chart.NoCategories);
    }

    [Fact]
    public void Category_labels_must_not_be_blank()
    {
        ChartBlock.Create(ChartType.Bar, ["ok", " "], [Series("s", 1d, 2d)], 10d, 10d)
            .Error.Should().Be(DomainErrors.Chart.EmptyCategory);
    }

    [Fact]
    public void A_chart_requires_at_least_one_series()
    {
        ChartBlock.Create(ChartType.Bar, ["A"], [], 10d, 10d)
            .Error.Should().Be(DomainErrors.Chart.NoSeries);
    }

    [Fact]
    public void Every_series_must_match_the_category_count()
    {
        ChartBlock.Create(ChartType.Bar, ["A", "B"], [Series("s", 1d)], 10d, 10d)
            .Error.Should().Be(DomainErrors.Chart.SeriesLengthMismatch);
    }

    [Fact]
    public void Chart_dimensions_must_be_positive()
    {
        ChartBlock.Create(ChartType.Bar, ["A"], [Series("s", 1d)], 0d, 10d)
            .Error.Should().Be(DomainErrors.Chart.NonPositiveDimension);
    }

    [Fact]
    public void A_pie_chart_must_have_exactly_one_series()
    {
        ChartBlock.Create(ChartType.Pie, ["A"], [Series("s1", 1d), Series("s2", 2d)], 10d, 10d)
            .Error.Should().Be(DomainErrors.Chart.PieRequiresSingleSeries);
    }

    [Fact]
    public void A_pie_chart_rejects_negative_values()
    {
        ChartBlock.Create(ChartType.Pie, ["A", "B"], [Series("s", 1d, -2d)], 10d, 10d)
            .Error.Should().Be(DomainErrors.Chart.PieRequiresNonNegativeValues);
    }

    [Fact]
    public void A_series_requires_a_name()
    {
        ChartSeries.Create(" ", [1d]).Error.Should().Be(DomainErrors.Chart.EmptySeriesName);
    }

    [Fact]
    public void A_series_requires_values()
    {
        ChartSeries.Create("s", []).Error.Should().Be(DomainErrors.Chart.EmptySeries);
    }
}
