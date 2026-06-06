using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>
/// A column in a <see cref="RowBlock"/> on a 12-unit grid (à la Bootstrap). A column either
/// has an explicit span (1-12) or is <em>auto</em>: auto columns share whatever grid units
/// the explicit columns leave free, split equally between them.
/// </summary>
public sealed class Column
{
    /// <summary>The number of grid units a full-width column spans.</summary>
    public const int MaxWidth = 12;

    private Column(int? width, IReadOnlyList<IBlock> blocks)
    {
        Width = width;
        Blocks = blocks;
    }

    /// <summary>The explicit span (1-12), or <see langword="null"/> for an auto column.</summary>
    public int? Width { get; }

    /// <summary>Whether this column's width is resolved from the row's free space.</summary>
    public bool IsAuto => Width is null;

    public IReadOnlyList<IBlock> Blocks { get; }

    /// <summary>Creates a column with an explicit span of <paramref name="width"/> units.</summary>
    public static Result<Column> Create(int width, IReadOnlyList<IBlock> blocks)
    {
        if (width is < 1 or > MaxWidth)
        {
            return DomainErrors.Grid.ColumnWidthOutOfRange;
        }

        return Build(width, blocks);
    }

    /// <summary>Creates an auto-width column that shares the row's remaining space.</summary>
    public static Result<Column> CreateAuto(IReadOnlyList<IBlock> blocks) => Build(null, blocks);

    private static Result<Column> Build(int? width, IReadOnlyList<IBlock> blocks)
    {
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
