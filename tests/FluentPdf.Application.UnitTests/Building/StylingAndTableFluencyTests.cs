using FluentPdf.Application.Building;
using FluentPdf.Domain;
using FluentPdf.Domain.Content;
using FluentPdf.Domain.Styling;

namespace FluentPdf.Application.UnitTests.Building;

public sealed class StylingAndTableFluencyTests
{
    private static readonly Color Navy = Color.FromHex("#1F3864").Value;

    [Fact]
    public void Table_rows_can_be_data_bound_from_a_sequence()
    {
        var data = new[] { ("a", 1), ("b", 2), ("c", 3) };

        var table = (TableBlock)BlockComposer.Compose(b => b
            .Table(t => t
                .Columns(2)
                .HeaderRow(r => r.Cell("name").Cell("n"))
                .Rows(data, (row, item) => row.Cell(item.Item1).Cell(item.Item2.ToString()))))
            .Value[0];

        // One header row plus one body row per item.
        table.Rows.Should().HaveCount(4);
    }

    [Fact]
    public void Data_bound_rows_propagate_a_failure()
    {
        BlockComposer.Compose(b => b
            .Table(t => t
                .Columns(1)
                .Rows(new[] { "ok", "" }, (row, item) => row.Cell(item))))
            .IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Data_bound_rows_with_a_null_sequence_fail()
    {
        BlockComposer.Compose(b => b
            .Table(t => t.Columns(1).Rows((string[])null!, (row, item) => row.Cell(item))))
            .IsFailure.Should().BeTrue();
    }

    [Fact]
    public void A_styled_cell_carries_the_style_on_its_paragraph()
    {
        var table = (TableBlock)BlockComposer.Compose(b => b
            .Table(t => t
                .Columns(1)
                .Row(r => r.Cell("99.00", TextStyle.Default.WithBold(), HorizontalAlignment.Right))))
            .Value[0];

        var cell = table.Rows[0].Cells[0];
        cell.Alignment.Should().Be(HorizontalAlignment.Right);
        var paragraph = cell.Blocks[0].Should().BeOfType<Paragraph>().Subject;
        paragraph.Runs[0].Style.IsBold.Should().BeTrue();
    }

    [Fact]
    public void A_styled_cell_propagates_invalid_text()
    {
        BlockComposer.Compose(b => b
            .Table(t => t.Columns(1).Row(r => r.Cell("", TextStyle.Default))))
            .Error.Should().Be(DomainErrors.TextRun.EmptyText);
    }

    [Fact]
    public void ParagraphBuilder_supports_coloured_and_underlined_runs()
    {
        var paragraph = (Paragraph)BlockComposer.Compose(b => b
            .Paragraph(p => p.Colored("red", Navy).Underline("u")))
            .Value[0];

        paragraph.Runs.Should().HaveCount(2);
        paragraph.Runs[0].Style.Color.Should().Be(Navy);
        paragraph.Runs[1].Style.IsUnderlined.Should().BeTrue();
    }

    [Fact]
    public void ParagraphBuilder_can_style_a_run_fluently()
    {
        var paragraph = (Paragraph)BlockComposer.Compose(b => b
            .Paragraph(p => p.Run("title", s => s.Size(16d).Bold().Color(Navy))))
            .Value[0];

        var style = paragraph.Runs[0].Style;
        style.FontSize.Should().Be(16d);
        style.IsBold.Should().BeTrue();
        style.Color.Should().Be(Navy);
    }

    [Fact]
    public void ParagraphBuilder_run_with_a_null_style_configurator_fails()
    {
        BlockComposer.Compose(b => b.Paragraph(p => p.Run("x", (Action<TextStyleBuilder>)null!)))
            .IsFailure.Should().BeTrue();
    }

    [Fact]
    public void TextStyleBuilder_composes_every_decoration()
    {
        var style = new TextStyleBuilder()
            .Font("Times")
            .Size(14d)
            .Bold()
            .Italic()
            .Underline()
            .Color(Navy)
            .Build();

        style.FontFamily.Should().Be("Times");
        style.FontSize.Should().Be(14d);
        style.IsBold.Should().BeTrue();
        style.IsItalic.Should().BeTrue();
        style.IsUnderlined.Should().BeTrue();
        style.Color.Should().Be(Navy);
    }

    [Fact]
    public void StyleSet_resolves_defined_styles_by_name()
    {
        var styles = StyleSet.Create(set => set
            .Define("title", s => s.Size(16d).Bold().Color(Navy))
            .Define("plain", TextStyle.Default));

        styles["title"].FontSize.Should().Be(16d);
        styles.Get("plain").Should().Be(TextStyle.Default);
        styles.Contains("title").Should().BeTrue();
        styles.TryGet("title", out var resolved).Should().BeTrue();
        resolved.IsBold.Should().BeTrue();
    }

    [Fact]
    public void StyleSet_derives_a_variant_from_a_base_style()
    {
        var styles = StyleSet.Create(set => set
            .Define("title", s => s.Size(16d).Bold().Color(Navy))
            .Define("subtitle", "title", s => s.Size(12d).Bold(false)));

        var subtitle = styles["subtitle"];
        subtitle.FontSize.Should().Be(12d);
        subtitle.IsBold.Should().BeFalse();
        // Inherited from the base style.
        subtitle.Color.Should().Be(Navy);
    }

    [Fact]
    public void StyleSet_throws_for_an_unknown_style_but_TryGet_does_not()
    {
        var styles = StyleSet.Create(set => set.Define("title", TextStyle.Default));

        styles.Invoking(s => s["missing"]).Should().Throw<KeyNotFoundException>();
        styles.TryGet("missing", out _).Should().BeFalse();
        styles.Contains("missing").Should().BeFalse();
    }

    [Fact]
    public void StyleSet_create_with_a_null_configurator_throws()
    {
        var act = () => StyleSet.Create(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
