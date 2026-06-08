using System.Globalization;
using FluentPdf.Application.Building;
using FluentPdf.Domain;
using FluentPdf.Domain.Content;
using FluentPdf.Domain.Styling;

namespace FluentPdf.Application.UnitTests.Building;

public sealed class DataTableTests
{
    private sealed record Row(string Name, decimal Amount, int Count);

    private static readonly Row[] Sample =
    [
        new("Alpha", 1.5m, 3),
        new("Beta", -2m, 7),
    ];

    private static TableBlock BuildTable(DataTable<Row> table) =>
        (TableBlock)table.Build(Sample).Value[0];

    private static string CellText(TableCell cell) =>
        ((Paragraph)cell.Blocks[0]).Runs[0].Text;

    private static TextStyle CellStyle(TableCell cell) =>
        ((Paragraph)cell.Blocks[0]).Runs[0].Style;

    [Fact]
    public void Columns_infer_alignment_from_the_value_type()
    {
        var table = BuildTable(DataTable.For<Row>(t => t
            .Column("Name", r => r.Name)
            .Column("Amount", r => r.Amount)
            .Column("Count", r => r.Count)));

        table.ColumnCount.Should().Be(3);

        var body = table.Rows[1];
        body.Cells[0].Alignment.Should().Be(HorizontalAlignment.Left);   // string
        body.Cells[1].Alignment.Should().Be(HorizontalAlignment.Right);  // decimal
        body.Cells[2].Alignment.Should().Be(HorizontalAlignment.Right);  // int
    }

    [Fact]
    public void The_first_row_is_a_repeatable_header()
    {
        var table = BuildTable(DataTable.For<Row>(t => t.Column("Name", r => r.Name)));

        table.Rows[0].IsHeader.Should().BeTrue();
        CellText(table.Rows[0].Cells[0]).Should().Be("Name");
        table.Rows[1].IsHeader.Should().BeFalse();
    }

    [Fact]
    public void An_explicit_alignment_overrides_the_inferred_one()
    {
        var table = BuildTable(DataTable.For<Row>(t => t
            .Column("Amount", r => r.Amount, HorizontalAlignment.Left)));

        table.Rows[1].Cells[0].Alignment.Should().Be(HorizontalAlignment.Left);
    }

    [Fact]
    public void Values_use_invariant_ToString_by_default()
    {
        var table = BuildTable(DataTable.For<Row>(t => t.Column("Amount", r => r.Amount)));

        CellText(table.Rows[1].Cells[0]).Should().Be("1.5");
    }

    [Fact]
    public void A_format_function_controls_the_display_text()
    {
        var table = BuildTable(DataTable.For<Row>(t => t
            .Column("Amount", r => r.Amount, v => v.ToString("0.00", CultureInfo.InvariantCulture))));

        CellText(table.Rows[1].Cells[0]).Should().Be("1.50");
    }

    [Fact]
    public void Highlight_rules_style_only_matching_rows()
    {
        var adverse = TextStyle.Default.WithBold();

        var table = BuildTable(DataTable.For<Row>(t => t
            .Column("Name", r => r.Name)
            .HighlightWhen(r => r.Amount < 0, adverse)));

        // Alpha (1.5) is unstyled; Beta (-2) is highlighted.
        CellStyle(table.Rows[1].Cells[0]).Should().Be(TextStyle.Default);
        CellStyle(table.Rows[2].Cells[0]).Should().Be(adverse);
    }

    [Fact]
    public void The_first_matching_highlight_rule_wins()
    {
        var first = TextStyle.Default.WithBold();
        var second = TextStyle.Default.WithItalic();

        var table = BuildTable(DataTable.For<Row>(t => t
            .Column("Name", r => r.Name)
            .HighlightWhen(r => r.Amount < 0, first)
            .HighlightWhen(r => r.Amount < 0, second)));

        CellStyle(table.Rows[2].Cells[0]).Should().Be(first);
    }

    [Fact]
    public void A_null_model_fails()
    {
        DataTable.For<Row>(t => t.Column("Name", r => r.Name))
            .Build(null!)
            .IsFailure.Should().BeTrue();
    }

    [Fact]
    public void A_table_with_no_columns_fails()
    {
        DataTable.For<Row>(_ => { })
            .Build(Sample)
            .IsFailure.Should().BeTrue();
    }

    [Fact]
    public void An_empty_cell_value_propagates_as_a_failure()
    {
        DataTable.For<Row>(t => t.Column("Name", _ => string.Empty))
            .Build(Sample)
            .Error.Should().Be(DomainErrors.TextRun.EmptyText);
    }

    [Fact]
    public void For_with_a_null_configurator_throws()
    {
        var act = () => DataTable.For<Row>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void The_table_overload_binds_a_collection_inline()
    {
        var result = BlockComposer.Compose(b => b
            .Table(Sample, t => t
                .Column("Name", r => r.Name)
                .Column("Amount", r => r.Amount)));

        var table = (TableBlock)result.Value[0];
        table.Rows.Should().HaveCount(3); // header + two data rows
    }

    [Fact]
    public void The_table_overload_with_a_null_sequence_fails()
    {
        BlockComposer.Compose(b => b.Table((Row[])null!, t => t.Column("Name", r => r.Name)))
            .IsFailure.Should().BeTrue();
    }

    [Fact]
    public void A_data_table_is_reusable_as_a_component()
    {
        var table = DataTable.For<Row>(t => t.Column("Name", r => r.Name));

        var result = BlockComposer.Compose(b => b.Component<IReadOnlyList<Row>>(table, Sample));

        result.Value.Should().ContainSingle();
    }
}
