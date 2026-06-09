using System.Globalization;
using FluentPdf.Domain.Content;
using FluentPdf.Domain.Styling;
using FluentPdf.Kernel;

namespace FluentPdf.Application.Building;

/// <summary>
/// Entry point for the column-oriented table component. Declare each column once — its header
/// and how to read its value — and the rows are derived from a collection automatically, instead
/// of declaring the column structure twice (header row + body rows) and keeping them in sync.
/// </summary>
public static class DataTable
{
    /// <summary>Composes a reusable, data-bound table component for a row type.</summary>
    public static DataTable<T> For<T>(Action<DataTableBuilder<T>> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new DataTableBuilder<T>();
        configure(builder);
        return builder.Build();
    }
}

/// <summary>
/// A reusable, column-oriented table: a <see cref="IBlockComponent{TModel}"/> that renders a
/// collection of rows as a single <see cref="TableBlock"/>. Define it once and bind any matching
/// collection to it.
/// </summary>
/// <typeparam name="T">The row type the table binds to.</typeparam>
public sealed class DataTable<T> : IBlockComponent<IReadOnlyList<T>>
{
    private readonly IReadOnlyList<DataColumn<T>> _columns;
    private readonly IReadOnlyList<HighlightRule<T>> _highlights;

    internal DataTable(IReadOnlyList<DataColumn<T>> columns, IReadOnlyList<HighlightRule<T>> highlights)
    {
        _columns = columns;
        _highlights = highlights;
    }

    /// <summary>Renders the rows as a table, surfacing the first error (if any) as a failure.</summary>
    public Result<IReadOnlyList<IBlock>> Build(IReadOnlyList<T> model)
    {
        if (model is null)
        {
            return Error.NullValue;
        }

        List<Result<TableRow>> rows = [HeaderRow()];

        foreach (var item in model)
        {
            rows.Add(BodyRow(item));
        }

        var collected = ResultList.Collect(rows);

        if (collected.IsFailure)
        {
            return collected.Error;
        }

        var table = TableBlock.Create(_columns.Count, collected.Value);

        if (table.IsFailure)
        {
            return table.Error;
        }

        IReadOnlyList<IBlock> blocks = [table.Value];
        return Result.Success(blocks);
    }

    private Result<TableRow> HeaderRow()
    {
        var cells = ResultList.Collect(_columns.Select(column => TableCell.FromText(column.Header, column.Alignment)));

        return cells.IsFailure
            ? cells.Error
            : TableRow.Create(cells.Value, isHeader: true);
    }

    private Result<TableRow> BodyRow(T item)
    {
        var rowStyle = MatchHighlight(item);
        var cells = ResultList.Collect(_columns.Select(column => BuildCell(column, item, rowStyle)));

        return cells.IsFailure
            ? cells.Error
            : TableRow.Create(cells.Value);
    }

    private TextStyle? MatchHighlight(T item)
    {
        foreach (var rule in _highlights)
        {
            if (rule.Predicate(item))
            {
                return rule.Style;
            }
        }

        return null;
    }

    private static Result<TableCell> BuildCell(DataColumn<T> column, T item, TextStyle? rowStyle)
    {
        var text = column.Value(item);

        // No highlight: build exactly as Cell(text, alignment) would, so a plain data table is
        // byte-identical to the row-oriented form.
        if (rowStyle is null)
        {
            return TableCell.FromText(text, column.Alignment);
        }

        var paragraph = Paragraph.FromText(text, rowStyle);

        return paragraph.IsFailure
            ? Result.Failure<TableCell>(paragraph.Error)
            : TableCell.Create([paragraph.Value], column.Alignment);
    }
}

/// <summary>
/// Fluent builder for a <see cref="DataTable{T}"/>. A column is just a header and how to read its
/// value; alignment is inferred from the value type (numbers right, everything else left) and can
/// be overridden. Conditional row styling is a separate, named rule via <see cref="HighlightWhen"/>.
/// </summary>
public sealed class DataTableBuilder<T>
{
    private readonly List<DataColumn<T>> _columns = [];
    private readonly List<HighlightRule<T>> _highlights = [];

    /// <summary>
    /// Adds a column with auto-inferred alignment (numeric values are right-aligned). An optional
    /// <paramref name="format"/> turns the typed value into display text; without one the value's
    /// invariant <c>ToString</c> is used.
    /// </summary>
    public DataTableBuilder<T> Column<TValue>(
        string header,
        Func<T, TValue> value,
        Func<TValue, string>? format = null) =>
        AddColumn(header, value, InferAlignment(typeof(TValue)), format);

    /// <summary>Adds a column with an explicit alignment.</summary>
    public DataTableBuilder<T> Column<TValue>(
        string header,
        Func<T, TValue> value,
        HorizontalAlignment alignment,
        Func<TValue, string>? format = null) =>
        AddColumn(header, value, alignment, format);

    /// <summary>
    /// Styles every cell of a row whose data matches <paramref name="predicate"/>. Rules are
    /// evaluated in declaration order and the first match wins, so conditional formatting reads as
    /// a named rule rather than a lambda buried in each column.
    /// </summary>
    public DataTableBuilder<T> HighlightWhen(Func<T, bool> predicate, TextStyle style)
    {
        if (predicate is not null && style is not null)
        {
            _highlights.Add(new HighlightRule<T>(predicate, style));
        }

        return this;
    }

    internal DataTable<T> Build() => new(_columns, _highlights);

    private DataTableBuilder<T> AddColumn<TValue>(
        string header,
        Func<T, TValue> value,
        HorizontalAlignment alignment,
        Func<TValue, string>? format)
    {
        string Producer(T item) => FormatValue(value(item), format);

        _columns.Add(new DataColumn<T>(header, Producer, alignment));
        return this;
    }

    private static string FormatValue<TValue>(TValue value, Func<TValue, string>? format)
    {
        if (format is not null)
        {
            return format(value);
        }

        return value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value?.ToString() ?? string.Empty;
    }

    private static HorizontalAlignment InferAlignment(Type valueType)
    {
        var type = Nullable.GetUnderlyingType(valueType) ?? valueType;
        return IsNumeric(type) ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    }

    private static bool IsNumeric(Type type) => Type.GetTypeCode(type) is
        TypeCode.Byte or TypeCode.SByte
        or TypeCode.Int16 or TypeCode.UInt16
        or TypeCode.Int32 or TypeCode.UInt32
        or TypeCode.Int64 or TypeCode.UInt64
        or TypeCode.Single or TypeCode.Double or TypeCode.Decimal;
}

/// <summary>A single declared column: header, value projection and resolved alignment.</summary>
internal sealed record DataColumn<T>(string Header, Func<T, string> Value, HorizontalAlignment Alignment);

/// <summary>A conditional row-styling rule.</summary>
internal sealed record HighlightRule<T>(Func<T, bool> Predicate, TextStyle Style);
