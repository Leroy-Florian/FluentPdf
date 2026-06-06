using FluentPdf.Application.Layout;
using FluentPdf.Domain.Styling;

namespace FluentPdf.Infrastructure.Rendering;

/// <summary>
/// A dependency-free, allocation-free <see cref="ITextMeasurer"/> that approximates text
/// metrics from the font size with simple average-advance factors. It is fast enough for
/// high-volume pagination and gives consistent page counts without embedding a font engine.
/// Adapters backed by a real layout library should supply their own measurer for exact
/// breaks; this is the sensible default.
/// </summary>
public sealed class ApproximateTextMeasurer : ITextMeasurer
{
    private readonly double _lineHeightFactor;
    private readonly double _averageCharWidthFactor;
    private readonly double _spaceWidthFactor;
    private readonly double _boldFactor;

    /// <summary>Creates a measurer, optionally tuning the average-advance factors.</summary>
    public ApproximateTextMeasurer(
        double lineHeightFactor = 1.2d,
        double averageCharWidthFactor = 0.5d,
        double spaceWidthFactor = 0.25d,
        double boldFactor = 1.05d)
    {
        _lineHeightFactor = lineHeightFactor;
        _averageCharWidthFactor = averageCharWidthFactor;
        _spaceWidthFactor = spaceWidthFactor;
        _boldFactor = boldFactor;
    }

    /// <inheritdoc />
    public double LineHeight(TextStyle style) => style.FontSize * _lineHeightFactor;

    /// <inheritdoc />
    public double SpaceWidth(TextStyle style) => style.FontSize * _spaceWidthFactor * Weight(style);

    /// <inheritdoc />
    public double MeasureWord(ReadOnlySpan<char> word, TextStyle style) =>
        word.Length * style.FontSize * _averageCharWidthFactor * Weight(style);

    private double Weight(TextStyle style) => style.IsBold ? _boldFactor : 1d;
}
