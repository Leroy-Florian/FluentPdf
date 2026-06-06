using FluentPdf.Kernel;

namespace FluentPdf.Domain.Layout;

/// <summary>
/// The size of a page expressed in PostScript points (1 point = 1/72 inch), the unit the
/// PDF format itself uses. Dimensions are always stored in portrait form (width &lt;= height
/// for the named presets) and reoriented via <see cref="WithOrientation"/>.
/// </summary>
public sealed class PageSize : ValueObject
{
    private PageSize(double width, double height)
    {
        Width = width;
        Height = height;
    }

    public double Width { get; }

    public double Height { get; }

    /// <summary>The orientation implied by the current width/height relationship.</summary>
    public PageOrientation Orientation =>
        Width > Height ? PageOrientation.Landscape : PageOrientation.Portrait;

    /// <summary>ISO A4 (210 × 297 mm).</summary>
    public static PageSize A4 { get; } = new(595.28d, 841.89d);

    /// <summary>ISO A3 (297 × 420 mm).</summary>
    public static PageSize A3 { get; } = new(841.89d, 1190.55d);

    /// <summary>ISO A5 (148 × 210 mm).</summary>
    public static PageSize A5 { get; } = new(419.53d, 595.28d);

    /// <summary>US Letter (8.5 × 11 in).</summary>
    public static PageSize Letter { get; } = new(612d, 792d);

    /// <summary>US Legal (8.5 × 14 in).</summary>
    public static PageSize Legal { get; } = new(612d, 1008d);

    /// <summary>Creates a custom page size from explicit point dimensions.</summary>
    public static Result<PageSize> Create(double width, double height)
    {
        if (width <= 0d || height <= 0d)
        {
            return DomainErrors.PageSize.NonPositiveDimension;
        }

        return new PageSize(width, height);
    }

    /// <summary>
    /// Returns this size oriented as requested, swapping width and height when needed.
    /// </summary>
    public PageSize WithOrientation(PageOrientation orientation)
    {
        var shouldBeLandscape = orientation == PageOrientation.Landscape;
        var isLandscape = Width > Height;

        return shouldBeLandscape == isLandscape ? this : new PageSize(Height, Width);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Width;
        yield return Height;
    }
}
