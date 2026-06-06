using FluentPdf.Application.Rendering;
using FluentPdf.Domain;
using FluentPdf.Kernel;

namespace FluentPdf.Application.UnitTests;

/// <summary>A hand-written renderer fake with configurable capabilities (no mocking framework).</summary>
internal sealed class TestRenderer : IPdfRenderer
{
    public TestRenderer(RendererCapabilities capabilities) =>
        Descriptor = new RendererDescriptor("Test", capabilities);

    public bool WasCalled { get; private set; }

    public RendererDescriptor Descriptor { get; }

    public Result<RenderedPdf> Render(PdfDocument document)
    {
        WasCalled = true;
        return RenderedPdf.Create([0x25, 0x50], pageCount: 1);
    }
}
