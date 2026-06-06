using FluentPdf.Adapters.QuestPdf;
using FluentPdf.Application.Rendering;
using FluentPdf.Conformance;

namespace FluentPdf.Adapters.QuestPdf.ConformanceTests;

/// <summary>Proves the QuestPDF adapter satisfies the shared renderer contract.</summary>
public sealed class QuestPdfRendererConformanceTests : PdfRendererContractTests
{
    protected override IPdfRenderer CreateRenderer() => new QuestPdfRenderer();

    protected override string ExtractText(IReadOnlyList<byte> content) => PdfText.Extract(content);
}
