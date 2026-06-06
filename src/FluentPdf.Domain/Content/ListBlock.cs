using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>An ordered or unordered list of <see cref="ListItem"/>s.</summary>
public sealed class ListBlock : IBlock
{
    private ListBlock(ListStyle style, IReadOnlyList<ListItem> items, int startNumber)
    {
        Style = style;
        Items = items;
        StartNumber = startNumber;
    }

    public ListStyle Style { get; }

    public IReadOnlyList<ListItem> Items { get; }

    /// <summary>
    /// The number the first item is labelled with in an ordered list (default 1). This lets a
    /// list that is split across pages continue its numbering on the next fragment.
    /// </summary>
    public int StartNumber { get; }

    /// <summary>Creates a list from its items, optionally starting an ordered list at an offset.</summary>
    public static Result<ListBlock> Create(
        ListStyle style,
        IReadOnlyList<ListItem> items,
        int startNumber = 1)
    {
        if (items is null || items.Count == 0)
        {
            return DomainErrors.ListBlock.NoItems;
        }

        if (items.Any(static item => item is null))
        {
            return Error.NullValue;
        }

        if (startNumber < 1)
        {
            return DomainErrors.ListBlock.InvalidStartNumber;
        }

        return new ListBlock(style, [.. items], startNumber);
    }
}
