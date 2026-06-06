using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>
/// A named series of numeric values plotted against a chart's categories. The values are
/// carried as plain numbers — never as pixels — so each adapter draws them with its own
/// charting library.
/// </summary>
public sealed class ChartSeries
{
    private readonly double[] _values;

    private ChartSeries(string name, double[] values)
    {
        Name = name;
        _values = values;
    }

    public string Name { get; }

    /// <summary>The data points, one per chart category.</summary>
    public IReadOnlyList<double> Values => _values;

    /// <summary>Creates a series, validating its name and that it carries at least one value.</summary>
    public static Result<ChartSeries> Create(string name, IReadOnlyList<double> values)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.Chart.EmptySeriesName;
        }

        if (values is null || values.Count == 0)
        {
            return DomainErrors.Chart.EmptySeries;
        }

        return new ChartSeries(name.Trim(), [.. values]);
    }
}
