using FluentPdf.Domain.Content;
using FluentPdf.Kernel;

namespace FluentPdf.Application.Building;

/// <summary>Fluent builder for an ordered or unordered <see cref="ListBlock"/>.</summary>
public sealed class ListBuilder
{
    private readonly ListStyle _style;
    private readonly List<Result<ListItem>> _items = [];

    internal ListBuilder(ListStyle style) => _style = style;

    /// <summary>Adds an item containing a single paragraph of text.</summary>
    public ListBuilder Item(string text)
    {
        _items.Add(ListItem.FromText(text));
        return this;
    }

    /// <summary>Adds an item with rich block content (e.g. a nested list).</summary>
    public ListBuilder Item(Action<ListItemBuilder> configure)
    {
        var builder = new ListItemBuilder();
        configure(builder);
        var blocks = builder.BuildBlocks();

        _items.Add(blocks.IsFailure
            ? Result.Failure<ListItem>(blocks.Error)
            : ListItem.Create(blocks.Value));

        return this;
    }

    /// <summary>
    /// Adds one item per element of <paramref name="items"/>, binding a collection straight into
    /// the list without an out-of-band <c>foreach</c>. A null sequence or body surfaces as a
    /// single failure.
    /// </summary>
    public ListBuilder ForEach<T>(IEnumerable<T> items, Action<ListBuilder, T> body)
    {
        if (items is null || body is null)
        {
            _items.Add(Result.Failure<ListItem>(Error.NullValue));
            return this;
        }

        foreach (var item in items)
        {
            body(this, item);
        }

        return this;
    }

    internal Result<ListBlock> Build()
    {
        var items = ResultList.Collect(_items);

        if (items.IsFailure)
        {
            return items.Error;
        }

        return ListBlock.Create(_style, items.Value);
    }
}

/// <summary>Fluent builder for the content of a single <see cref="ListItem"/>.</summary>
public sealed class ListItemBuilder : BlockContainerBuilder<ListItemBuilder>
{
}
