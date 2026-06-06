using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>
/// A single entry in a <see cref="ListBlock"/>. An item holds one or more blocks, which
/// allows nested lists and multi-paragraph items.
/// </summary>
public sealed class ListItem
{
    private ListItem(IReadOnlyList<IBlock> blocks) => Blocks = blocks;

    public IReadOnlyList<IBlock> Blocks { get; }

    /// <summary>Creates a list item from its block content.</summary>
    public static Result<ListItem> Create(IReadOnlyList<IBlock> blocks)
    {
        if (blocks is null || blocks.Count == 0)
        {
            return DomainErrors.ListBlock.NoItems;
        }

        if (blocks.Any(static block => block is null))
        {
            return Error.NullValue;
        }

        return new ListItem([.. blocks]);
    }

    /// <summary>Creates a list item containing a single paragraph of text.</summary>
    public static Result<ListItem> FromText(string text)
    {
        var paragraph = Paragraph.FromText(text);

        if (paragraph.IsFailure)
        {
            return paragraph.Error;
        }

        return new ListItem([paragraph.Value]);
    }
}
