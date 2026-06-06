using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>
/// An agnostic chart: a type, an optional title, a set of category labels and one or more
/// <see cref="ChartSeries"/>, plus an intrinsic size in points. It describes <em>what</em> to
/// plot, never how it is painted, so every adapter renders it with its own charting library
/// (and an adapter that cannot must declare the chart capability unsupported).
/// </summary>
public sealed class ChartBlock : IBlock
{
    private ChartBlock(
        ChartType type,
        string? title,
        IReadOnlyList<string> categories,
        IReadOnlyList<ChartSeries> series,
        double width,
        double height)
    {
        Type = type;
        Title = title;
        Categories = categories;
        Series = series;
        Width = width;
        Height = height;
    }

    public ChartType Type { get; }

    /// <summary>The chart's title, or <see langword="null"/> when untitled.</summary>
    public string? Title { get; }

    /// <summary>The category labels shared by every series (x-axis / slice labels).</summary>
    public IReadOnlyList<string> Categories { get; }

    public IReadOnlyList<ChartSeries> Series { get; }

    /// <summary>The intrinsic width, in points.</summary>
    public double Width { get; }

    /// <summary>The intrinsic height, in points.</summary>
    public double Height { get; }

    /// <summary>
    /// Creates a chart, validating that it has categories and series, that every series
    /// supplies exactly one value per category, that the size is positive, and that pie
    /// charts carry a single non-negative series.
    /// </summary>
    public static Result<ChartBlock> Create(
        ChartType type,
        IReadOnlyList<string> categories,
        IReadOnlyList<ChartSeries> series,
        double width,
        double height,
        string? title = null)
    {
        if (categories is null || categories.Count == 0)
        {
            return DomainErrors.Chart.NoCategories;
        }

        if (categories.Any(string.IsNullOrWhiteSpace))
        {
            return DomainErrors.Chart.EmptyCategory;
        }

        if (series is null || series.Count == 0)
        {
            return DomainErrors.Chart.NoSeries;
        }

        if (series.Any(static s => s is null))
        {
            return Error.NullValue;
        }

        if (series.Any(s => s.Values.Count != categories.Count))
        {
            return DomainErrors.Chart.SeriesLengthMismatch;
        }

        if (width <= 0d || height <= 0d)
        {
            return DomainErrors.Chart.NonPositiveDimension;
        }

        if (type == ChartType.Pie && series.Count != 1)
        {
            return DomainErrors.Chart.PieRequiresSingleSeries;
        }

        if (type == ChartType.Pie && series[0].Values.Any(static value => value < 0d))
        {
            return DomainErrors.Chart.PieRequiresNonNegativeValues;
        }

        var normalisedTitle = string.IsNullOrWhiteSpace(title) ? null : title!.Trim();

        return new ChartBlock(type, normalisedTitle, [.. categories], [.. series], width, height);
    }
}
