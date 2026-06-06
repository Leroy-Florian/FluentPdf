using FluentPdf.Domain.Content;
using FluentPdf.Kernel;

namespace FluentPdf.Application.Building;

/// <summary>Fluent builder for a <see cref="RowBlock"/> in the 12-unit grid.</summary>
public sealed class RowBuilder
{
    private readonly List<Result<Column>> _columns = [];

    /// <summary>Adds a column spanning <paramref name="width"/> grid units (1-12).</summary>
    public RowBuilder Column(int width, Action<ColumnBuilder> configure) =>
        AddColumn(blocks => Domain.Content.Column.Create(width, blocks), configure);

    /// <summary>Adds an auto-width column that shares the row's remaining grid space.</summary>
    public RowBuilder Column(Action<ColumnBuilder> configure) =>
        AddColumn(Domain.Content.Column.CreateAuto, configure);

    private RowBuilder AddColumn(
        Func<IReadOnlyList<IBlock>, Result<Column>> create,
        Action<ColumnBuilder> configure)
    {
        var builder = new ColumnBuilder();
        configure(builder);
        var blocks = builder.BuildBlocks();

        _columns.Add(blocks.IsFailure
            ? Result.Failure<Column>(blocks.Error)
            : create(blocks.Value));

        return this;
    }

    internal Result<RowBlock> Build()
    {
        var columns = ResultList.Collect(_columns);

        if (columns.IsFailure)
        {
            return columns.Error;
        }

        return RowBlock.Create(columns.Value);
    }
}

/// <summary>Fluent builder for the content of a single grid <see cref="Column"/>.</summary>
public sealed class ColumnBuilder : BlockContainerBuilder<ColumnBuilder>
{
}
