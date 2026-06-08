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

    /// <summary>
    /// Adds one body row per item in <paramref name="items"/>, binding the data straight into
    /// the table without an out-of-band <c>foreach</c>. A null sequence or row builder surfaces
    /// as a single failure.
    /// </summary>
    public TableBuilder Rows<T>(IEnumerable<T> items, Action<TableRowBuilder, T> row)
    {
        if (items is null || row is null)
        {
            _rows.Add(Result.Failure<TableRow>(Error.NullValue));
            return this;
        }

        foreach (var item in items)
        {
            var builder = new TableRowBuilder();
            row(builder, item);
            _rows.Add(builder.Build(false));
        }

        return this;
    }

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

    /// <summary>
    /// Adds a cell containing a single styled paragraph. Saves dropping into the full
    /// <see cref="Cell(Action{CellBuilder})"/> form just to apply a text style — the verbose
    /// pattern that recurs across data tables (right-aligned, coloured amounts and totals).
    /// </summary>
    public TableRowBuilder Cell(
        string text,
        TextStyle? style,
        HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        // Mirror Cell(text, alignment): the cell carries the alignment, the paragraph keeps the
        // default left flow — only the run's style differs.
        var paragraph = Paragraph.FromText(text, style);

        _cells.Add(paragraph.IsFailure
            ? Result.Failure<TableCell>(paragraph.Error)
            : TableCell.Create([paragraph.Value], alignment));

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
