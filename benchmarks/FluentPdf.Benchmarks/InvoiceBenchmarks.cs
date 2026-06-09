using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using FluentPdf.Adapters.IText;
using FluentPdf.Adapters.QuestPdf;
using FluentPdf.Application.Rendering;
using FluentPdf.Application.UseCases;
using FluentPdf.Benchmarks.Raw;
using FluentPdf.Infrastructure.Rendering;
using FluentPdf.Samples.Invoice;

namespace FluentPdf.Benchmarks;

/// <summary>
/// The light, single-page document: it isolates the fixed per-render cost of the FluentPdf
/// pipeline (build the agnostic model, scan its features, translate it through the adapter)
/// against driving QuestPDF and iText directly. The ratio column is the abstraction tax.
/// </summary>
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class InvoiceBenchmarks
{
    private readonly InvoiceDto _invoice = InvoiceSample.SampleData();
    private readonly InvoiceTemplate _template = new();
    private IPdfRenderer _quest = null!;
    private IPdfRenderer _itext = null!;
    private IPdfRenderer _inMemory = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Constructing the QuestPDF adapter runs its static constructor, which registers the
        // embedded fonts and the Community licence the raw QuestPDF twin also relies on.
        _quest = new QuestPdfRenderer();
        _itext = new ITextRenderer();
        _inMemory = new InMemoryPdfRenderer();

        BenchmarkSupport.EnsureEquivalent("Invoice/QuestPDF", QuestPdfRawDocuments.Invoice(_invoice), Render(_quest));
        BenchmarkSupport.EnsureEquivalent("Invoice/iText", new ITextRawDocuments().Invoice(_invoice), Render(_itext));
        _ = Render(_inMemory);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("QuestPDF")]
    public byte[] QuestPdf_Raw() => QuestPdfRawDocuments.Invoice(_invoice);

    [Benchmark]
    [BenchmarkCategory("QuestPDF")]
    public RenderedPdf QuestPdf_FluentPdf() => Render(_quest);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("iText")]
    public byte[] IText_Raw() => new ITextRawDocuments().Invoice(_invoice);

    [Benchmark]
    [BenchmarkCategory("iText")]
    public RenderedPdf IText_FluentPdf() => Render(_itext);

    [Benchmark]
    [BenchmarkCategory("Pipeline")]
    public RenderedPdf Pipeline_InMemory() => Render(_inMemory);

    private RenderedPdf Render(IPdfRenderer renderer) =>
        new RenderDocumentUseCase(renderer).Execute(_template.Build(_invoice).Value).Value;
}
