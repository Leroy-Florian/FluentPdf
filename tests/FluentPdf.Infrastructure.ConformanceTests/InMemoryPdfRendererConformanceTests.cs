using System.Text;
using FluentPdf.Application.Rendering;
using FluentPdf.Conformance;
using FluentPdf.Infrastructure.Rendering;

namespace FluentPdf.Infrastructure.ConformanceTests;

/// <summary>
/// Proves the in-memory adapter satisfies the shared renderer contract. Writing an adapter
/// for a real library (iText, QuestPDF, …) means writing a class exactly like this one.
/// </summary>
public sealed class InMemoryPdfRendererConformanceTests : PdfRendererContractTests
{
    protected override IPdfRenderer CreateRenderer() => new InMemoryPdfRenderer();

    protected override string ExtractText(IReadOnlyList<byte> content) =>
        Encoding.UTF8.GetString([.. content]);
}
