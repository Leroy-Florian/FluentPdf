using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>A row of a <see cref="TableBlock"/>, holding one cell per column.</summary>
public sealed class TableRow
{
    private TableRow(IReadOnlyList<TableCell> cells, bool isHeader)
    {
        Cells = cells;
        IsHeader = isHeader;
    }

    public IReadOnlyList<TableCell> Cells { get; }

    /// <summary>Whether this row is a header row (repeated when the table spans pages).</summary>
    public bool IsHeader { get; }

    /// <summary>Creates a row from its cells.</summary>
    public static Result<TableRow> Create(IReadOnlyList<TableCell> cells, bool isHeader = false)
    {
        if (cells is null || cells.Count == 0)
        {
            return DomainErrors.Table.NoColumns;
        }

        if (cells.Any(static cell => cell is null))
        {
            return Error.NullValue;
        }

        return new TableRow([.. cells], isHeader);
    }
}
