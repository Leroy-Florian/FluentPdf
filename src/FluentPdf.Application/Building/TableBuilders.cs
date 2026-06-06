using FluentPdf.Domain.Content;
using FluentPdf.Domain.Styling;
using FluentPdf.Kernel;

namespace FluentPdf.Application.Building;

/// <summary>Fluent builder for a <see cref="TableBlock"/>.</summary>
public sealed class TableBuilder
{
    private readonly List<Result<TableRow>> _rows = [];
    private int _columnCount;

    /// <summary>Declares the number of columns every row must supply.</summary>
    public TableBuilder Columns(int count)
    {
        _columnCount = count;
        return this;
    }

    /// <summary>Adds a header row (repeated when the table spans pages).</summary>
    public TableBuilder HeaderRow(Action<TableRowBuilder> configure) => AddRow(true, configure);

    /// <summary>Adds a body row.</summary>
    public TableBuilder Row(Action<TableRowBuilder> configure) => AddRow(false, configure);

    internal Result<TableBlock> Build()
    {
        var rows = ResultList.Collect(_rows);

        if (rows.IsFailure)
        {
            return rows.Error;
        }

        return TableBlock.Create(_columnCount, rows.Value);
    }

    private TableBuilder AddRow(bool isHeader, Action<TableRowBuilder> configure)
    {
        var builder = new TableRowBuilder();
        configure(builder);
        _rows.Add(builder.Build(isHeader));
        return this;
    }
}

/// <summary>Fluent builder for a single <see cref="TableRow"/>.</summary>
public sealed class TableRowBuilder
{
    private readonly List<Result<TableCell>> _cells = [];

    /// <summary>Adds a cell containing a single paragraph of text.</summary>
    public TableRowBuilder Cell(string text, HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        _cells.Add(TableCell.FromText(text, alignment));
        return this;
    }

    /// <summary>Adds a cell with rich block content.</summary>
    public TableRowBuilder Cell(Action<CellBuilder> configure)
    {
        var builder = new CellBuilder();
        configure(builder);
        var blocks = builder.BuildBlocks();

        _cells.Add(blocks.IsFailure
            ? Result.Failure<TableCell>(blocks.Error)
            : TableCell.Create(blocks.Value, builder.Alignment));

        return this;
    }

    internal Result<TableRow> Build(bool isHeader)
    {
        var cells = ResultList.Collect(_cells);

        if (cells.IsFailure)
        {
            return cells.Error;
        }

        return TableRow.Create(cells.Value, isHeader);
    }
}

/// <summary>Fluent builder for the content of a single <see cref="TableCell"/>.</summary>
public sealed class CellBuilder : BlockContainerBuilder<CellBuilder>
{
    /// <summary>The horizontal alignment applied to the cell's content.</summary>
    public HorizontalAlignment Alignment { get; private set; } = HorizontalAlignment.Left;

    /// <summary>Sets the cell's horizontal alignment.</summary>
    public CellBuilder Align(HorizontalAlignment alignment)
    {
        Alignment = alignment;
        return this;
    }
}
