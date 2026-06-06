using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>
/// A horizontal row of <see cref="Column"/>s on a 12-unit grid (à la Bootstrap). Explicit
/// column widths must not exceed 12 combined; any free units are shared equally between the
/// auto-width columns. A row of only explicit columns may use fewer than 12 units (the rest
/// stays empty).
/// </summary>
public sealed class RowBlock : IBlock
{
    private RowBlock(IReadOnlyList<Column> columns) => Columns = columns;

    public IReadOnlyList<Column> Columns { get; }

    /// <summary>The grid units consumed by the explicit (non-auto) columns.</summary>
    public int UsedWidth => Columns.Where(static c => !c.IsAuto).Sum(static c => c.Width!.Value);

    /// <summary>The number of auto-width columns in the row.</summary>
    public int AutoColumnCount => Columns.Count(static c => c.IsAuto);

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

        var explicitWidth = columns.Where(static c => !c.IsAuto).Sum(static c => c.Width!.Value);

        if (explicitWidth > Column.MaxWidth)
        {
            return DomainErrors.Grid.RowOverflow;
        }

        var hasAuto = columns.Any(static c => c.IsAuto);

        if (hasAuto && explicitWidth >= Column.MaxWidth)
        {
            return DomainErrors.Grid.NoSpaceForAutoColumns;
        }

        return new RowBlock([.. columns]);
    }

    /// <summary>
    /// Resolves the effective width, in grid units, of every column — explicit columns keep
    /// their span; auto columns receive an equal share of the remaining units.
    /// </summary>
    public IReadOnlyList<double> ResolveWidths()
    {
        var autoCount = AutoColumnCount;
        var autoWidth = autoCount > 0 ? (Column.MaxWidth - UsedWidth) / (double)autoCount : 0d;

        return [.. Columns.Select(column => column.IsAuto ? autoWidth : column.Width!.Value)];
    }
}
