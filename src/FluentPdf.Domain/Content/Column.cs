using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>
/// A column in a <see cref="RowBlock"/>, occupying a span of a 12-unit grid (à la
/// Bootstrap) and holding its own block content.
/// </summary>
public sealed class Column
{
    /// <summary>The number of grid units a full-width column spans.</summary>
    public const int MaxWidth = 12;

    private Column(int width, IReadOnlyList<IBlock> blocks)
    {
        Width = width;
        Blocks = blocks;
    }

    /// <summary>The column span, between 1 and <see cref="MaxWidth"/>.</summary>
    public int Width { get; }

    public IReadOnlyList<IBlock> Blocks { get; }

    /// <summary>Creates a column of the given span with its block content.</summary>
    public static Result<Column> Create(int width, IReadOnlyList<IBlock> blocks)
    {
        if (width is < 1 or > MaxWidth)
        {
            return DomainErrors.Grid.ColumnWidthOutOfRange;
        }

        if (blocks is null || blocks.Count == 0)
        {
            return DomainErrors.Grid.EmptyColumn;
        }

        if (blocks.Any(static block => block is null))
        {
            return Error.NullValue;
        }

        return new Column(width, [.. blocks]);
    }
}
