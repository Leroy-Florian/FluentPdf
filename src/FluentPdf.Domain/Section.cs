using FluentPdf.Domain.Content;
using FluentPdf.Domain.Layout;
using FluentPdf.Kernel;

namespace FluentPdf.Domain;

/// <summary>
/// A contiguous range of pages sharing a page size, margins and optional header/footer.
/// A document is a sequence of one or more sections.
/// </summary>
public sealed class Section
{
    private Section(
        PageSize pageSize,
        Margins margins,
        PageFurniture? header,
        PageFurniture? footer,
        IReadOnlyList<IBlock> blocks)
    {
        PageSize = pageSize;
        Margins = margins;
        Header = header;
        Footer = footer;
        Blocks = blocks;
    }

    public PageSize PageSize { get; }

    public Margins Margins { get; }

    /// <summary>Optional running header repeated on every page of the section.</summary>
    public PageFurniture? Header { get; }

    /// <summary>Optional running footer repeated on every page of the section.</summary>
    public PageFurniture? Footer { get; }

    public IReadOnlyList<IBlock> Blocks { get; }

    /// <summary>Creates a section.</summary>
    public static Result<Section> Create(
        PageSize pageSize,
        Margins margins,
        IReadOnlyList<IBlock> blocks,
        PageFurniture? header = null,
        PageFurniture? footer = null)
    {
        if (pageSize is null || margins is null)
        {
            return Error.NullValue;
        }

        if (blocks is null || blocks.Count == 0)
        {
            return DomainErrors.Section.NoBlocks;
        }

        if (blocks.Any(static block => block is null))
        {
            return Error.NullValue;
        }

        return new Section(pageSize, margins, header, footer, [.. blocks]);
    }
}
