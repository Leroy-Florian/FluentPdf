using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>
/// Running content repeated on every page of a section — used for both headers and
/// footers. Holds one or more blocks.
/// </summary>
public sealed class PageFurniture
{
    private PageFurniture(IReadOnlyList<IBlock> blocks) => Blocks = blocks;

    public IReadOnlyList<IBlock> Blocks { get; }

    /// <summary>Creates running content from its blocks.</summary>
    public static Result<PageFurniture> Create(IReadOnlyList<IBlock> blocks)
    {
        if (blocks is null || blocks.Count == 0)
        {
            return DomainErrors.Section.NoBlocks;
        }

        if (blocks.Any(static block => block is null))
        {
            return Error.NullValue;
        }

        return new PageFurniture([.. blocks]);
    }
}
