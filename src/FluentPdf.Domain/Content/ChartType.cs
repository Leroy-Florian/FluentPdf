namespace FluentPdf.Domain.Content;

/// <summary>
/// The kind of chart to draw. Deliberately limited to the few families every charting
/// back-end can produce, so the agnostic model stays portable across adapters.
/// </summary>
public enum ChartType
{
    /// <summary>Vertical bars grouped per category.</summary>
    Bar = 0,

    /// <summary>A line per series across the categories.</summary>
    Line = 1,

    /// <summary>A single series split into proportional slices.</summary>
    Pie = 2,
}
