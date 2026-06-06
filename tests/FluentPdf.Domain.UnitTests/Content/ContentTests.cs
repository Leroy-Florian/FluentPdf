using FluentPdf.Domain;
using FluentPdf.Domain.Content;
using FluentPdf.Domain.Styling;

namespace FluentPdf.Domain.UnitTests.Content;

public sealed class ContentTests
{
    private static Paragraph SampleParagraph() => Paragraph.FromText("sample").Value;

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void TextRun_rejects_empty_text(string? text)
    {
        var result = TextRun.Create(text!);

        result.Error.Should().Be(DomainErrors.TextRun.EmptyText);
    }

    [Fact]
    public void TextRun_defaults_to_the_default_style()
    {
        TextRun.Create("hi").Value.Style.Should().Be(TextStyle.Default);
    }

    [Fact]
    public void TextRun_compares_by_value()
    {
        TextRun.Create("hi").Value.Should().Be(TextRun.Create("hi").Value);
    }

    [Fact]
    public void Paragraph_requires_at_least_one_run()
    {
        Paragraph.Create([]).Error.Should().Be(DomainErrors.Paragraph.NoRuns);
    }

    [Fact]
    public void Paragraph_rejects_null_runs()
    {
        var result = Paragraph.Create([null!]);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Paragraph_from_text_propagates_text_errors()
    {
        Paragraph.FromText("").IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Paragraph_keeps_runs_and_alignment()
    {
        var run = TextRun.Create("x").Value;

        var paragraph = Paragraph.Create([run], HorizontalAlignment.Center).Value;

        paragraph.Runs.Should().ContainSingle().Which.Should().Be(run);
        paragraph.Alignment.Should().Be(HorizontalAlignment.Center);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    public void Spacer_rejects_non_positive_height(double height)
    {
        Spacer.Create(height).Error.Should().Be(DomainErrors.Spacer.NonPositiveHeight);
    }

    [Fact]
    public void Spacer_keeps_height()
    {
        Spacer.Create(5d).Value.Height.Should().Be(5d);
    }

    [Fact]
    public void PageBreak_is_a_shared_instance()
    {
        PageBreak.Instance.Should().BeSameAs(PageBreak.Instance);
    }

    [Fact]
    public void Image_requires_data()
    {
        ImageBlock.Create([], ImageFormat.Png, 1d, 1d).Error
            .Should().Be(DomainErrors.Image.EmptyData);
    }

    [Theory]
    [InlineData(0d, 1d)]
    [InlineData(1d, 0d)]
    public void Image_requires_positive_dimensions(double width, double height)
    {
        ImageBlock.Create([1], ImageFormat.Png, width, height).Error
            .Should().Be(DomainErrors.Image.NonPositiveDimension);
    }

    [Fact]
    public void Image_copies_data_defensively()
    {
        var data = new byte[] { 1, 2, 3 };

        var image = ImageBlock.Create(data, ImageFormat.Jpeg, 10d, 20d).Value;
        data[0] = 99;

        image.Data.Should().Equal(1, 2, 3);
        image.Format.Should().Be(ImageFormat.Jpeg);
        image.Width.Should().Be(10d);
        image.Height.Should().Be(20d);
    }

    [Fact]
    public void List_requires_at_least_one_item()
    {
        ListBlock.Create(ListStyle.Ordered, []).Error.Should().Be(DomainErrors.ListBlock.NoItems);
    }

    [Fact]
    public void ListItem_from_text_wraps_a_paragraph()
    {
        ListItem.FromText("x").Value.Blocks.Should().ContainSingle();
    }

    [Fact]
    public void ListItem_requires_blocks()
    {
        ListItem.Create([]).Error.Should().Be(DomainErrors.ListBlock.NoItems);
    }

    [Fact]
    public void List_keeps_style_and_items()
    {
        var item = ListItem.FromText("a").Value;

        var list = ListBlock.Create(ListStyle.Unordered, [item]).Value;

        list.Style.Should().Be(ListStyle.Unordered);
        list.Items.Should().ContainSingle();
    }

    [Fact]
    public void Table_requires_columns()
    {
        var row = TableRow.Create([TableCell.FromText("a").Value]).Value;

        TableBlock.Create(0, [row]).Error.Should().Be(DomainErrors.Table.NoColumns);
    }

    [Fact]
    public void Table_requires_rows()
    {
        TableBlock.Create(1, []).Error.Should().Be(DomainErrors.Table.NoRows);
    }

    [Fact]
    public void Table_rejects_rows_with_the_wrong_width()
    {
        var row = TableRow.Create([TableCell.FromText("a").Value]).Value;

        TableBlock.Create(2, [row]).Error.Should().Be(DomainErrors.Table.RowWidthMismatch);
    }

    [Fact]
    public void Table_accepts_matching_rows()
    {
        var row = TableRow.Create(
        [
            TableCell.FromText("a").Value,
            TableCell.FromText("b").Value,
        ]).Value;

        var table = TableBlock.Create(2, [row]).Value;

        table.ColumnCount.Should().Be(2);
        table.Rows.Should().ContainSingle();
    }

    [Fact]
    public void TableCell_requires_content()
    {
        TableCell.Create([]).Error.Should().Be(DomainErrors.Table.EmptyCell);
    }

    [Fact]
    public void TableRow_requires_cells()
    {
        TableRow.Create([]).Error.Should().Be(DomainErrors.Table.NoColumns);
    }

    [Fact]
    public void TableRow_can_be_a_header()
    {
        TableRow.Create([TableCell.FromText("a").Value], isHeader: true).Value.IsHeader
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Column_rejects_widths_outside_one_to_twelve(int width)
    {
        Column.Create(width, [SampleParagraph()]).Error
            .Should().Be(DomainErrors.Grid.ColumnWidthOutOfRange);
    }

    [Fact]
    public void Column_requires_content()
    {
        Column.Create(6, []).Error.Should().Be(DomainErrors.Grid.EmptyColumn);
    }

    [Fact]
    public void Row_requires_columns()
    {
        RowBlock.Create([]).Error.Should().Be(DomainErrors.Grid.NoColumns);
    }

    [Fact]
    public void Row_rejects_overflowing_columns()
    {
        var wide = Column.Create(8, [SampleParagraph()]).Value;
        var alsoWide = Column.Create(8, [SampleParagraph()]).Value;

        RowBlock.Create([wide, alsoWide]).Error.Should().Be(DomainErrors.Grid.RowOverflow);
    }

    [Fact]
    public void Row_accepts_columns_within_the_grid()
    {
        var left = Column.Create(6, [SampleParagraph()]).Value;
        var right = Column.Create(6, [SampleParagraph()]).Value;

        var row = RowBlock.Create([left, right]).Value;

        row.Columns.Should().HaveCount(2);
        row.UsedWidth.Should().Be(12);
    }

    [Fact]
    public void Auto_column_is_marked_auto_and_has_no_explicit_width()
    {
        var column = Column.CreateAuto([SampleParagraph()]).Value;

        column.IsAuto.Should().BeTrue();
        column.Width.Should().BeNull();
    }

    [Fact]
    public void Explicit_column_is_not_auto()
    {
        Column.Create(4, [SampleParagraph()]).Value.IsAuto.Should().BeFalse();
    }

    [Fact]
    public void Auto_column_requires_content()
    {
        Column.CreateAuto([]).Error.Should().Be(DomainErrors.Grid.EmptyColumn);
    }

    [Fact]
    public void Auto_columns_share_the_remaining_grid_space()
    {
        var fixedColumn = Column.Create(6, [SampleParagraph()]).Value;
        var autoA = Column.CreateAuto([SampleParagraph()]).Value;
        var autoB = Column.CreateAuto([SampleParagraph()]).Value;

        var row = RowBlock.Create([fixedColumn, autoA, autoB]).Value;

        row.UsedWidth.Should().Be(6);
        row.AutoColumnCount.Should().Be(2);
        row.ResolveWidths().Should().Equal(6d, 3d, 3d);
    }

    [Fact]
    public void All_explicit_row_resolves_to_explicit_widths()
    {
        var row = RowBlock.Create([Column.Create(4, [SampleParagraph()]).Value]).Value;

        row.ResolveWidths().Should().Equal(4d);
    }

    [Fact]
    public void Auto_columns_fail_when_no_space_remains()
    {
        var full = Column.Create(12, [SampleParagraph()]).Value;
        var auto = Column.CreateAuto([SampleParagraph()]).Value;

        RowBlock.Create([full, auto]).Error.Should().Be(DomainErrors.Grid.NoSpaceForAutoColumns);
    }

    [Fact]
    public void An_ordered_list_can_start_at_an_offset()
    {
        var list = ListBlock.Create(
            ListStyle.Ordered,
            [ListItem.FromText("a").Value, ListItem.FromText("b").Value],
            startNumber: 5).Value;

        list.StartNumber.Should().Be(5);
    }

    [Fact]
    public void A_list_defaults_to_starting_at_one()
    {
        ListBlock.Create(ListStyle.Ordered, [ListItem.FromText("a").Value]).Value
            .StartNumber.Should().Be(1);
    }

    [Fact]
    public void A_non_positive_start_number_fails()
    {
        ListBlock.Create(ListStyle.Ordered, [ListItem.FromText("a").Value], startNumber: 0)
            .Error.Should().Be(DomainErrors.ListBlock.InvalidStartNumber);
    }

    [Fact]
    public void PageFurniture_requires_blocks()
    {
        PageFurniture.Create([]).Error.Should().Be(DomainErrors.Section.NoBlocks);
    }

    [Fact]
    public void PageFurniture_keeps_blocks()
    {
        PageFurniture.Create([SampleParagraph()]).Value.Blocks.Should().ContainSingle();
    }
}
