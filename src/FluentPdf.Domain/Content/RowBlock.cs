using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>
/// A horizontal row of <see cref="Column"/>s laid out on a 12-unit grid (à la Bootstrap).
/// The combined column width must not exceed 12; any remaining units are left empty, which
/// allows partial rows.
/// </summary>
public sealed class RowBlock : IBlock
{
    private RowBlock(IReadOnlyList<Column> columns) => Columns = columns;

    public IReadOnlyList<Column> Columns { get; }

    /// <summary>The total grid units occupied by the row's columns.</summary>
    public int UsedWidth => Columns.Sum(static column => column.Width);

    /// <summary>Creates a row, validating that the columns fit within the grid.</summary>
    public static Result<RowBlock> Create(IReadOnlyList<Column> columns)
    {
        if (columns is null || columns.Count == 0)
        {
            return DomainErrors.Grid.NoColumns;
        }

        if (columns.Any(static column => column is null))
        {
            return Error.NullValue;
        }

        if (columns.Sum(static column => column.Width) > Column.MaxWidth)
        {
            return DomainErrors.Grid.RowOverflow;
        }

        return new RowBlock([.. columns]);
    }
}
