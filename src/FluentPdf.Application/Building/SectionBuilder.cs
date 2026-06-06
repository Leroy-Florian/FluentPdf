using FluentPdf.Domain;
using FluentPdf.Domain.Content;
using FluentPdf.Domain.Layout;
using FluentPdf.Kernel;
using LayoutMargins = FluentPdf.Domain.Layout.Margins;
using LayoutPageSize = FluentPdf.Domain.Layout.PageSize;

namespace FluentPdf.Application.Building;

/// <summary>
/// Fluent builder for a <see cref="Section"/>: a run of pages sharing a page size, margins
/// and optional header/footer, plus the section's block content.
/// </summary>
public sealed class SectionBuilder : BlockContainerBuilder<SectionBuilder>
{
    private static readonly LayoutMargins DefaultMargins = LayoutMargins.Uniform(72d).Value;

    private LayoutPageSize _pageSize = LayoutPageSize.A4;
    private Result<LayoutMargins> _margins = DefaultMargins;
    private PageOrientation _orientation = PageOrientation.Portrait;
    private FurnitureBuilder? _header;
    private FurnitureBuilder? _footer;

    /// <summary>Sets the page size (default A4).</summary>
    public SectionBuilder PageSize(LayoutPageSize size)
    {
        _pageSize = size;
        return this;
    }

    /// <summary>Sets the page orientation (default portrait).</summary>
    public SectionBuilder Orientation(PageOrientation orientation)
    {
        _orientation = orientation;
        return this;
    }

    /// <summary>Sets portrait orientation.</summary>
    public SectionBuilder Portrait() => Orientation(PageOrientation.Portrait);

    /// <summary>Sets landscape orientation.</summary>
    public SectionBuilder Landscape() => Orientation(PageOrientation.Landscape);

    /// <summary>Sets explicit margins.</summary>
    public SectionBuilder Margins(LayoutMargins margins)
    {
        _margins = margins;
        return this;
    }

    /// <summary>Sets uniform margins on every side, in points.</summary>
    public SectionBuilder Margins(double uniform)
    {
        _margins = LayoutMargins.Uniform(uniform);
        return this;
    }

    /// <summary>Defines the running header repeated on every page of the section.</summary>
    public SectionBuilder Header(Action<FurnitureBuilder> configure)
    {
        _header = new FurnitureBuilder();
        configure(_header);
        return this;
    }

    /// <summary>Defines the running footer repeated on every page of the section.</summary>
    public SectionBuilder Footer(Action<FurnitureBuilder> configure)
    {
        _footer = new FurnitureBuilder();
        configure(_footer);
        return this;
    }

    internal Result<Section> Build()
    {
        var blocks = BuildBlocks();

        if (blocks.IsFailure)
        {
            return blocks.Error;
        }

        if (_margins.IsFailure)
        {
            return _margins.Error;
        }

        var header = BuildFurniture(_header);

        if (header.IsFailure)
        {
            return header.Error;
        }

        var footer = BuildFurniture(_footer);

        if (footer.IsFailure)
        {
            return footer.Error;
        }

        var oriented = _pageSize.WithOrientation(_orientation);

        return Section.Create(oriented, _margins.Value, blocks.Value, header.Value, footer.Value);
    }

    private static Result<PageFurniture?> BuildFurniture(FurnitureBuilder? builder)
    {
        if (builder is null)
        {
            return Result.Success<PageFurniture?>(null);
        }

        var blocks = builder.BuildBlocks();

        if (blocks.IsFailure)
        {
            return Result.Failure<PageFurniture?>(blocks.Error);
        }

        var furniture = PageFurniture.Create(blocks.Value);

        return furniture.IsFailure
            ? Result.Failure<PageFurniture?>(furniture.Error)
            : Result.Success<PageFurniture?>(furniture.Value);
    }
}
