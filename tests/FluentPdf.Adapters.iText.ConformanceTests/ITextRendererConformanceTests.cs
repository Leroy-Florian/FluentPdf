using FluentPdf.Adapters.IText;
using FluentPdf.Application.Rendering;
using FluentPdf.Conformance;

namespace FluentPdf.Adapters.IText.ConformanceTests;

/// <summary>Proves the iText adapter satisfies the shared renderer contract.</summary>
public sealed class ITextRendererConformanceTests : PdfRendererContractTests
{
    protected override IPdfRenderer CreateRenderer() => new ITextRenderer();

    protected override string ExtractText(IReadOnlyList<byte> content) => PdfText.Extract(content);
}
