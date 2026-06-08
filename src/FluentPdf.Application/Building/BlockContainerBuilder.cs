using FluentPdf.Domain.Content;
using FluentPdf.Domain.Styling;
using FluentPdf.Kernel;

namespace FluentPdf.Application.Building;

/// <summary>
/// Base class for every fluent builder that accumulates block-level content (sections,
/// grid columns, table cells, list items, headers and footers). Uses the curiously
/// recurring template pattern so each method returns the concrete builder for chaining.
/// </summary>
/// <typeparam name="TSelf">The concrete builder type.</typeparam>
public abstract class BlockContainerBuilder<TSelf>
    where TSelf : BlockContainerBuilder<TSelf>
{
    private readonly List<Result<IReadOnlyList<IBlock>>> _items = [];

    private TSelf Self => (TSelf)this;

    /// <summary>Appends an already-built block (e.g. a reusable, prebuilt instance).</summary>
    public TSelf Add(IBlock block)
    {
        AddBlock(block is null
            ? Result.Failure<IBlock>(Error.NullValue)
            : Result.Success(block));
        return Self;
    }

    /// <summary>Appends a sequence of already-built, reusable blocks.</summary>
    public TSelf Blocks(IEnumerable<IBlock> blocks)
    {
        if (blocks is null)
        {
            AddBlock(Result.Failure<IBlock>(Error.NullValue));
            return Self;
        }

        foreach (var block in blocks)
        {
            Add(block);
        }

        return Self;
    }

    /// <summary>Appends a single-run paragraph from text.</summary>
    public TSelf Paragraph(
        string text,
        TextStyle? style = null,
        HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        AddBlock(Domain.Content.Paragraph.FromText(text, style, alignment).AsBlock());
        return Self;
    }

    /// <summary>
    /// Appends a heading paragraph (bold by default) optionally followed by vertical spacing.
    /// A convenience over <see cref="Paragraph(string, TextStyle, HorizontalAlignment)"/> for
    /// the ubiquitous "title then gap" pattern, so callers stop hand-writing a paragraph plus a
    /// magic-number <see cref="Spacer"/> every time.
    /// </summary>
    public TSelf Heading(
        string text,
        TextStyle? style = null,
        HorizontalAlignment alignment = HorizontalAlignment.Left,
        double spacingAfter = 0d)
    {
        AddBlock(Domain.Content.Paragraph
            .FromText(text, style ?? TextStyle.Default.WithBold(), alignment)
            .AsBlock());

        if (spacingAfter > 0d)
        {
            Spacer(spacingAfter);
        }

        return Self;
    }

    /// <summary>Appends a paragraph built from multiple styled runs.</summary>
    public TSelf Paragraph(Action<ParagraphBuilder> configure)
    {
        var builder = new ParagraphBuilder();
        configure(builder);
        AddBlock(builder.Build().AsBlock());
        return Self;
    }

    /// <summary>
    /// Appends a dynamic page-number field (e.g. "Page {page} of {pages}"), resolved during
    /// pagination. Typically used inside a running header or footer.
    /// </summary>
    public TSelf PageNumber(
        string format,
        HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        AddBlock(Domain.Content.PageNumberField.Create(format, alignment).AsBlock());
        return Self;
    }

    /// <summary>Appends vertical whitespace of the given height, in points.</summary>
    public TSelf Spacer(double height)
    {
        AddBlock(Domain.Content.Spacer.Create(height).AsBlock());
        return Self;
    }

    /// <summary>Appends a page break.</summary>
    public TSelf PageBreak()
    {
        AddBlock(Result.Success<IBlock>(Domain.Content.PageBreak.Instance));
        return Self;
    }

    /// <summary>Appends an embedded image.</summary>
    public TSelf Image(
        byte[] data,
        ImageFormat format,
        double width,
        double height,
        HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        AddBlock(ImageBlock.Create(data, format, width, height, alignment).AsBlock());
        return Self;
    }

    /// <summary>Appends a Bootstrap-style grid row of columns.</summary>
    public TSelf Row(Action<RowBuilder> configure)
    {
        var builder = new RowBuilder();
        configure(builder);
        AddBlock(builder.Build().AsBlock());
        return Self;
    }

    /// <summary>Appends an unordered (bulleted) list.</summary>
    public TSelf UnorderedList(Action<ListBuilder> configure) =>
        AddList(ListStyle.Unordered, configure);

    /// <summary>Appends an ordered (numbered) list.</summary>
    public TSelf OrderedList(Action<ListBuilder> configure) =>
        AddList(ListStyle.Ordered, configure);

    /// <summary>Appends a table.</summary>
    public TSelf Table(Action<TableBuilder> configure)
    {
        var builder = new TableBuilder();
        configure(builder);
        AddBlock(builder.Build().AsBlock());
        return Self;
    }

    /// <summary>Appends an agnostic chart (bar, line or pie).</summary>
    public TSelf Chart(Action<ChartBuilder> configure)
    {
        var builder = new ChartBuilder();
        configure(builder);
        AddBlock(builder.Build().AsBlock());
        return Self;
    }

    /// <summary>Appends the blocks produced by a reusable component.</summary>
    public TSelf Component(IBlockComponent component)
    {
        AddBlocks(component is null
            ? Result.Failure<IReadOnlyList<IBlock>>(Error.NullValue)
            : component.Build());
        return Self;
    }

    /// <summary>Appends the blocks produced by a reusable, DTO-driven component.</summary>
    public TSelf Component<TModel>(IBlockComponent<TModel> component, TModel model)
    {
        AddBlocks(component is null
            ? Result.Failure<IReadOnlyList<IBlock>>(Error.NullValue)
            : component.Build(model));
        return Self;
    }

    /// <summary>Appends the blocks produced by an inline component delegate.</summary>
    public TSelf Component(Func<Result<IReadOnlyList<IBlock>>> component)
    {
        AddBlocks(component is null
            ? Result.Failure<IReadOnlyList<IBlock>>(Error.NullValue)
            : component());
        return Self;
    }

    /// <summary>Appends the blocks produced by an inline, DTO-driven component delegate.</summary>
    public TSelf Component<TModel>(TModel model, Func<TModel, Result<IReadOnlyList<IBlock>>> component)
    {
        AddBlocks(component is null
            ? Result.Failure<IReadOnlyList<IBlock>>(Error.NullValue)
            : component(model));
        return Self;
    }

    /// <summary>
    /// Repeats <paramref name="body"/> for each item in <paramref name="items"/> without
    /// breaking the fluent chain — the ergonomic replacement for stepping out of the builder
    /// into a <c>foreach</c> loop. A null sequence or body surfaces as a single failure.
    /// </summary>
    public TSelf ForEach<TItem>(IEnumerable<TItem> items, Action<TSelf, TItem> body)
    {
        if (items is null || body is null)
        {
            AddBlock(Result.Failure<IBlock>(Error.NullValue));
            return Self;
        }

        foreach (var item in items)
        {
            body(Self, item);
        }

        return Self;
    }

    /// <summary>
    /// Runs <paramref name="body"/> only when <paramref name="condition"/> holds, keeping
    /// conditional content inside the fluent chain instead of an out-of-band <c>if</c>.
    /// </summary>
    public TSelf When(bool condition, Action<TSelf> body)
    {
        if (body is null)
        {
            AddBlock(Result.Failure<IBlock>(Error.NullValue));
            return Self;
        }

        if (condition)
        {
            body(Self);
        }

        return Self;
    }

    /// <summary>Runs <paramref name="body"/> only when <paramref name="condition"/> is false.</summary>
    public TSelf Unless(bool condition, Action<TSelf> body) => When(!condition, body);

    /// <summary>Appends the blocks produced by a reusable component for each model in turn.</summary>
    public TSelf Components<TModel>(IEnumerable<TModel> models, IBlockComponent<TModel> component)
    {
        if (models is null || component is null)
        {
            AddBlocks(Result.Failure<IReadOnlyList<IBlock>>(Error.NullValue));
            return Self;
        }

        foreach (var model in models)
        {
            Component(component, model);
        }

        return Self;
    }

    internal Result<IReadOnlyList<IBlock>> BuildBlocks()
    {
        var blocks = new List<IBlock>();

        foreach (var item in _items)
        {
            if (item.IsFailure)
            {
                return item.Error;
            }

            blocks.AddRange(item.Value);
        }

        IReadOnlyList<IBlock> readOnly = blocks;
        return Result.Success(readOnly);
    }

    private TSelf AddList(ListStyle style, Action<ListBuilder> configure)
    {
        var builder = new ListBuilder(style);
        configure(builder);
        AddBlock(builder.Build().AsBlock());
        return Self;
    }

    private void AddBlock(Result<IBlock> block) =>
        AddBlocks(block.IsSuccess
            ? Result.Success<IReadOnlyList<IBlock>>([block.Value])
            : Result.Failure<IReadOnlyList<IBlock>>(block.Error));

    private void AddBlocks(Result<IReadOnlyList<IBlock>> blocks) => _items.Add(blocks);
}
