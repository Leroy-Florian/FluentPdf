using FluentPdf.Domain.Content;
using FluentPdf.Kernel;

namespace FluentPdf.Application.Building;

/// <summary>
/// Fluent builder for an agnostic <see cref="ChartBlock"/>: pick a type, declare the category
/// labels and add one or more named series. Errors (e.g. a series whose length does not match
/// the categories) are surfaced as a single failed result from the enclosing
/// <see cref="PdfDocumentBuilder.Build"/>, never thrown.
/// </summary>
public sealed class ChartBuilder
{
    private readonly List<string> _categories = [];
    private readonly List<Result<ChartSeries>> _series = [];
    private ChartType _type = ChartType.Bar;
    private string? _title;
    private double _width = 480d;
    private double _height = 240d;

    /// <summary>Selects the chart type (default <see cref="ChartType.Bar"/>).</summary>
    public ChartBuilder Type(ChartType type)
    {
        _type = type;
        return this;
    }

    /// <summary>Draws the chart as grouped vertical bars.</summary>
    public ChartBuilder Bar() => Type(ChartType.Bar);

    /// <summary>Draws the chart as one line per series.</summary>
    public ChartBuilder Line() => Type(ChartType.Line);

    /// <summary>Draws the chart as a single-series pie.</summary>
    public ChartBuilder Pie() => Type(ChartType.Pie);

    /// <summary>Sets the chart's title.</summary>
    public ChartBuilder Title(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>Sets the intrinsic size, in points (default 480 × 240).</summary>
    public ChartBuilder Size(double width, double height)
    {
        _width = width;
        _height = height;
        return this;
    }

    /// <summary>Declares the category labels shared by every series.</summary>
    public ChartBuilder Categories(params string[] categories)
    {
        _categories.AddRange(categories);
        return this;
    }

    /// <summary>Adds a named series from a sequence of values.</summary>
    public ChartBuilder Series(string name, params double[] values)
    {
        _series.Add(ChartSeries.Create(name, values));
        return this;
    }

    /// <summary>Adds a named series from a value sequence (e.g. a projected collection).</summary>
    public ChartBuilder Series(string name, IEnumerable<double> values)
    {
        _series.Add(ChartSeries.Create(name, [.. values]));
        return this;
    }

    internal Result<ChartBlock> Build()
    {
        var series = ResultList.Collect(_series);

        if (series.IsFailure)
        {
            return series.Error;
        }

        return ChartBlock.Create(_type, _categories, series.Value, _width, _height, _title);
    }
}
