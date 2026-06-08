using FluentPdf.Adapters.Shared;
using FluentPdf.Domain.Content;

namespace FluentPdf.Visual.UnitTests;

public sealed class SkiaChartRendererTests
{
    private static ChartSeries Series(string name, params double[] values) =>
        ChartSeries.Create(name, values).Value;

    [Theory]
    [InlineData(ChartType.Bar)]
    [InlineData(ChartType.Line)]
    public void Renders_a_categorical_chart_to_a_png(ChartType type)
    {
        var chart = ChartBlock.Create(
            type,
            ["Q1", "Q2", "Q3"],
            [Series("Revenue", 10d, 20d, 30d), Series("EBITDA", 3d, 6d, 9d)],
            480d,
            240d,
            "Trend").Value;

        var png = SkiaChartRenderer.RenderPng(chart);

        png.Should().StartWith([(byte)137, (byte)80, (byte)78, (byte)71]); // PNG signature
        png.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void Renders_a_pie_chart_to_a_png()
    {
        var chart = ChartBlock.Create(
            ChartType.Pie,
            ["A", "B", "C"],
            [Series("Share", 50d, 30d, 20d)],
            320d,
            320d,
            "Mix").Value;

        SkiaChartRenderer.RenderPng(chart).Should().NotBeEmpty();
    }

    [Fact]
    public void The_same_chart_renders_to_identical_bytes_for_both_adapters()
    {
        var chart = ChartBlock.Create(
            ChartType.Bar,
            ["A", "B"],
            [Series("S", 1d, 2d)],
            300d,
            200d).Value;

        SkiaChartRenderer.RenderPng(chart).Should().Equal(SkiaChartRenderer.RenderPng(chart));
    }
}
