using FluentPdf.Application.Layout;
using FluentPdf.Domain.Styling;

namespace FluentPdf.Application.UnitTests.Layout;

/// <summary>
/// A hand-written <see cref="ITextMeasurer"/> fake with fixed metrics, used to make
/// pagination deterministic in tests. It also counts word measurements so tests can prove the
/// paginator streams (does less work to produce the first page than the whole document).
/// </summary>
internal sealed class FixedTextMeasurer(
    double lineHeight = 12d,
    double spaceWidth = 3d,
    double charWidth = 6d) : ITextMeasurer
{
    public int WordMeasurements { get; private set; }

    public double LineHeight(TextStyle style) => lineHeight;

    public double SpaceWidth(TextStyle style) => spaceWidth;

    public double MeasureWord(ReadOnlySpan<char> word, TextStyle style)
    {
        WordMeasurements++;
        return word.Length * charWidth;
    }
}

/// <summary>An <see cref="ITextMeasurer"/> that throws if used, to prove pagination is lazy.</summary>
internal sealed class ThrowingTextMeasurer : ITextMeasurer
{
    public double LineHeight(TextStyle style) => throw new InvalidOperationException("measured");

    public double SpaceWidth(TextStyle style) => throw new InvalidOperationException("measured");

    public double MeasureWord(ReadOnlySpan<char> word, TextStyle style) =>
        throw new InvalidOperationException("measured");
}
