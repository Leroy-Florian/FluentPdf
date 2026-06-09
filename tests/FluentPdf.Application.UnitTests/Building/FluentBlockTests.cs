using FluentPdf.Application.Building;
using FluentPdf.Domain;
using FluentPdf.Domain.Content;
using FluentPdf.Domain.Styling;
using FluentPdf.Kernel;

namespace FluentPdf.Application.UnitTests.Building;

public sealed class FluentBlockTests
{
    [Fact]
    public void ForEach_appends_blocks_for_each_item_without_breaking_the_chain()
    {
        var result = BlockComposer.Compose(b => b
            .Paragraph("intro")
            .ForEach(new[] { "a", "b", "c" }, (blocks, item) => blocks.Paragraph(item)));

        result.Value.Should().HaveCount(4);
    }

    [Fact]
    public void ForEach_propagates_the_first_failure()
    {
        var result = BlockComposer.Compose(b => b
            .ForEach(new[] { "ok", "" }, (blocks, item) => blocks.Paragraph(item)));

        result.Error.Should().Be(DomainErrors.TextRun.EmptyText);
    }

    [Fact]
    public void ForEach_with_a_null_sequence_fails()
    {
        BlockComposer.Compose(b => b.ForEach((string[])null!, (blocks, item) => blocks.Paragraph(item)))
            .IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ForEach_with_a_null_body_fails()
    {
        BlockComposer.Compose(b => b.ForEach(new[] { "a" }, null!))
            .IsFailure.Should().BeTrue();
    }

    [Fact]
    public void When_runs_the_body_only_if_the_condition_holds()
    {
        var included = BlockComposer.Compose(b => b.When(true, blocks => blocks.Paragraph("x")));
        var skipped = BlockComposer.Compose(b => b.Paragraph("base").When(false, blocks => blocks.Paragraph("x")));

        included.Value.Should().ContainSingle();
        skipped.Value.Should().ContainSingle();
    }

    [Fact]
    public void Unless_is_the_inverse_of_when()
    {
        var ran = BlockComposer.Compose(b => b.Unless(false, blocks => blocks.Paragraph("x")));
        var skipped = BlockComposer.Compose(b => b.Paragraph("base").Unless(true, blocks => blocks.Paragraph("x")));

        ran.Value.Should().ContainSingle();
        skipped.Value.Should().ContainSingle();
    }

    [Fact]
    public void When_with_a_null_body_fails()
    {
        BlockComposer.Compose(b => b.When(true, null!)).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Components_renders_the_component_once_per_model()
    {
        var component = new EchoComponent();

        var result = BlockComposer.Compose(b => b.Components(new[] { "a", "b" }, component));

        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public void Components_with_a_null_sequence_fails()
    {
        BlockComposer.Compose(b => b.Components<string>(null!, new EchoComponent()))
            .IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Components_with_a_null_component_fails()
    {
        BlockComposer.Compose(b => b.Components(new[] { "a" }, (IBlockComponent<string>)null!))
            .IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Heading_is_bold_by_default_and_adds_trailing_spacing()
    {
        var blocks = BlockComposer.Compose(b => b.Heading("Title", spacingAfter: 6d)).Value;

        blocks.Should().HaveCount(2);
        var heading = blocks[0].Should().BeOfType<Paragraph>().Subject;
        heading.Runs[0].Style.IsBold.Should().BeTrue();
        blocks[1].Should().BeOfType<Spacer>().Which.Height.Should().Be(6d);
    }

    [Fact]
    public void Heading_without_spacing_adds_no_spacer()
    {
        var blocks = BlockComposer.Compose(b => b.Heading("Title")).Value;

        blocks.Should().ContainSingle().Which.Should().BeOfType<Paragraph>();
    }

    [Fact]
    public void Heading_honours_an_explicit_style_and_alignment()
    {
        var style = TextStyle.Default.WithFontSize(20d);

        var heading = (Paragraph)BlockComposer
            .Compose(b => b.Heading("Title", style, HorizontalAlignment.Center))
            .Value[0];

        heading.Alignment.Should().Be(HorizontalAlignment.Center);
        heading.Runs[0].Style.FontSize.Should().Be(20d);
        heading.Runs[0].Style.IsBold.Should().BeFalse();
    }

    private sealed class EchoComponent : IBlockComponent<string>
    {
        public Result<IReadOnlyList<IBlock>> Build(string model) =>
            BlockComposer.Compose(b => b.Paragraph(model));
    }
}
