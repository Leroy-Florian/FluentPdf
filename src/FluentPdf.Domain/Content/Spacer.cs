using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>Vertical whitespace of a fixed height, in points.</summary>
public sealed class Spacer : IBlock
{
    private Spacer(double height) => Height = height;

    public double Height { get; }

    /// <summary>Creates a spacer of the given height.</summary>
    public static Result<Spacer> Create(double height)
    {
        if (height <= 0d)
        {
            return DomainErrors.Spacer.NonPositiveHeight;
        }

        return new Spacer(height);
    }
}
