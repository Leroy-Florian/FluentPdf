using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>
/// A grid of cells arranged in rows and a fixed number of columns. Every row must supply
/// exactly one cell per column so adapters can lay the table out unambiguously.
/// </summary>
public sealed class TableBlock : IBlock
{
    private TableBlock(int columnCount, IReadOnlyList<TableRow> rows)
    {
        ColumnCount = columnCount;
        Rows = rows;
    }

    public int ColumnCount { get; }

    public IReadOnlyList<TableRow> Rows { get; }

    /// <summary>Creates a table, validating that every row matches the column count.</summary>
    public static Result<TableBlock> Create(int columnCount, IReadOnlyList<TableRow> rows)
    {
        if (columnCount <= 0)
        {
            return DomainErrors.Table.NoColumns;
        }

        if (rows is null || rows.Count == 0)
        {
            return DomainErrors.Table.NoRows;
        }

        if (rows.Any(static row => row is null))
        {
            return Error.NullValue;
        }

        if (rows.Any(row => row.Cells.Count != columnCount))
        {
            return DomainErrors.Table.RowWidthMismatch;
        }

        return new TableBlock(columnCount, [.. rows]);
    }
}
