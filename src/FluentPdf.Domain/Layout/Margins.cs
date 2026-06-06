using FluentPdf.Kernel;

namespace FluentPdf.Domain.Layout;

/// <summary>
/// The page margins, in points. None of the four values may be negative.
/// </summary>
public sealed class Margins : ValueObject
{
    private Margins(double top, double right, double bottom, double left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public double Top { get; }

    public double Right { get; }

    public double Bottom { get; }

    public double Left { get; }

    /// <summary>Zero margins on every side.</summary>
    public static Margins None { get; } = new(0d, 0d, 0d, 0d);

    /// <summary>Creates margins with an explicit value per side.</summary>
    public static Result<Margins> Create(double top, double right, double bottom, double left)
    {
        if (top < 0d || right < 0d || bottom < 0d || left < 0d)
        {
            return DomainErrors.Margins.NegativeValue;
        }

        return new Margins(top, right, bottom, left);
    }

    /// <summary>Creates uniform margins with the same value on every side.</summary>
    public static Result<Margins> Uniform(double value) => Create(value, value, value, value);

    /// <summary>Creates symmetric margins (vertical for top/bottom, horizontal for left/right).</summary>
    public static Result<Margins> Symmetric(double vertical, double horizontal) =>
        Create(vertical, horizontal, vertical, horizontal);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Top;
        yield return Right;
        yield return Bottom;
        yield return Left;
    }
}
