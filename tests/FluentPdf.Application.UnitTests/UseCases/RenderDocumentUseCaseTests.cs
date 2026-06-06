using FluentPdf.Application.Building;
using FluentPdf.Application.Rendering;
using FluentPdf.Application.UseCases;
using FluentPdf.Domain;

namespace FluentPdf.Application.UnitTests.UseCases;

public sealed class RenderDocumentUseCaseTests
{
    private static PdfDocument BaselineDocument() =>
        PdfDocumentBuilder.Create()
            .Section(s => s.Paragraph("hello"))
            .Build()
            .Value;

    private static PdfDocument TableDocument() =>
        PdfDocumentBuilder.Create()
            .Section(s => s.Table(t => t.Columns(1).Row(r => r.Cell("a"))))
            .Build()
            .Value;

    [Fact]
    public void Null_renderer_is_rejected_in_the_constructor()
    {
        var act = () => new RenderDocumentUseCase(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Null_document_fails_without_calling_the_renderer()
    {
        var renderer = new TestRenderer(RendererCapabilities.Full);
        var useCase = new RenderDocumentUseCase(renderer);

        var result = useCase.Execute(null!);

        result.Error.Should().Be(RenderErrors.NullDocument);
        renderer.WasCalled.Should().BeFalse();
    }

    [Fact]
    public void Supported_document_is_rendered()
    {
        var renderer = new TestRenderer(RendererCapabilities.Full);
        var useCase = new RenderDocumentUseCase(renderer);

        var result = useCase.Execute(BaselineDocument());

        result.IsSuccess.Should().BeTrue();
        renderer.WasCalled.Should().BeTrue();
    }

    [Fact]
    public void Unsupported_feature_fails_fast_and_names_the_adapter()
    {
        // A renderer that only supports the baseline cannot render a table.
        var renderer = new TestRenderer(RendererCapabilities.Basic);
        var useCase = new RenderDocumentUseCase(renderer);

        var result = useCase.Execute(TableDocument());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Render.UnsupportedFeatures");
        result.Error.Message.Should().Contain("Test").And.Contain("Table");
        renderer.WasCalled.Should().BeFalse();
    }
}
