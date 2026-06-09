using FluentPdf.Adapters.PdfSharp;
using FluentPdf.Application.Rendering;
using FluentPdf.Conformance;

namespace FluentPdf.Adapters.PdfSharp.ConformanceTests;

/// <summary>Proves the PDFsharp/MigraDoc adapter satisfies the shared renderer contract.</summary>
public sealed class PdfSharpRendererConformanceTests : PdfRendererContractTests
{
    protected override IPdfRenderer CreateRenderer() => new PdfSharpRenderer();

    protected override string ExtractText(IReadOnlyList<byte> content) => PdfText.Extract(content);
}
