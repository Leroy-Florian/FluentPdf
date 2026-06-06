using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>An ordered or unordered list of <see cref="ListItem"/>s.</summary>
public sealed class ListBlock : IBlock
{
    private ListBlock(ListStyle style, IReadOnlyList<ListItem> items)
    {
        Style = style;
        Items = items;
    }

    public ListStyle Style { get; }

    public IReadOnlyList<ListItem> Items { get; }

    /// <summary>Creates a list from its items.</summary>
    public static Result<ListBlock> Create(ListStyle style, IReadOnlyList<ListItem> items)
    {
        if (items is null || items.Count == 0)
        {
            return DomainErrors.ListBlock.NoItems;
        }

        if (items.Any(static item => item is null))
        {
            return Error.NullValue;
        }

        return new ListBlock(style, [.. items]);
    }
}
