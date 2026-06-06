using FluentPdf.Application.Building;
using FluentPdf.Domain;
using FluentPdf.Domain.Content;
using FluentPdf.Kernel;

namespace FluentPdf.Application.UnitTests.Building;

public sealed class ComponentTests
{
    [Fact]
    public void BlockComposer_returns_the_composed_blocks()
    {
        var result = BlockComposer.Compose(b => b.Paragraph("a").Paragraph("b"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public void BlockComposer_surfaces_the_first_error()
    {
        var result = BlockComposer.Compose(b => b.Paragraph("ok").Spacer(0d));

        result.Error.Should().Be(DomainErrors.Spacer.NonPositiveHeight);
    }

    [Fact]
    public void A_reusable_component_can_be_added_to_a_section()
    {
        var component = new GreetingComponent();

        var document = PdfDocumentBuilder.Create()
            .Section(s => s.Component(component).Component(component))
            .Build()
            .Value;

        // The component contributes two blocks each time it is reused.
        document.Sections[0].Blocks.Should().HaveCount(4);
    }

    [Fact]
    public void A_failing_component_propagates_its_error()
    {
        var document = PdfDocumentBuilder.Create()
            .Section(s => s.Component(new FailingComponent()))
            .Build();

        document.Error.Should().Be(DomainErrors.Spacer.NonPositiveHeight);
    }

    [Fact]
    public void A_null_component_fails()
    {
        PdfDocumentBuilder.Create()
            .Section(s => s.Component((IBlockComponent)null!))
            .Build()
            .IsFailure.Should().BeTrue();
    }

    [Fact]
    public void A_dto_driven_template_renders_the_model()
    {
        var template = new BadgeTemplate();

        var first = template.Build(new Badge("Alice"));
        var second = template.Build(new Badge("Bob"));

        first.Value.Sections[0].Blocks.Should().ContainSingle();
        second.IsSuccess.Should().BeTrue();
    }

    private sealed class GreetingComponent : IBlockComponent
    {
        public Result<IReadOnlyList<IBlock>> Build() =>
            BlockComposer.Compose(b => b.Paragraph("hello").Spacer(1d));
    }

    private sealed class FailingComponent : IBlockComponent
    {
        public Result<IReadOnlyList<IBlock>> Build() =>
            BlockComposer.Compose(b => b.Spacer(0d));
    }

    private sealed record Badge(string Name);

    private sealed class BadgeComponent : IBlockComponent<Badge>
    {
        public Result<IReadOnlyList<IBlock>> Build(Badge model) =>
            BlockComposer.Compose(b => b.Paragraph($"Hello, {model.Name}"));
    }

    private sealed class BadgeTemplate : IDocumentTemplate<Badge>
    {
        private readonly BadgeComponent _badge = new();

        public Result<PdfDocument> Build(Badge model) =>
            PdfDocumentBuilder.Create()
                .Section(s => s.Component(_badge, model))
                .Build();
    }
}
