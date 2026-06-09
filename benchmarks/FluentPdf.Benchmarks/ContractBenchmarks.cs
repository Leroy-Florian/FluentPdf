using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using FluentPdf.Adapters.IText;
using FluentPdf.Adapters.QuestPdf;
using FluentPdf.Application.Rendering;
using FluentPdf.Application.UseCases;
using FluentPdf.Benchmarks.Raw;
using FluentPdf.Infrastructure.Rendering;
using FluentPdf.Samples.Contract;

namespace FluentPdf.Benchmarks;

/// <summary>
/// The heavy, multi-page document (a ~35-page agreement with running furniture and automatic
/// "Page X of Y" numbering). Here the FluentPdf cost that scales with content — building a large
/// agnostic tree, the streaming paginator, the two-pass footer resolution — is put head to head
/// with hand-written QuestPDF and iText that paginate the same agreement directly.
/// </summary>
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class ContractBenchmarks
{
    private readonly ContractDto _contract = ContractText.Sample();
    private readonly ContractTemplate _template = new();
    private IPdfRenderer _quest = null!;
    private IPdfRenderer _itext = null!;
    private IPdfRenderer _inMemory = null!;

    [GlobalSetup]
    public void Setup()
    {
        _quest = new QuestPdfRenderer();
        _itext = new ITextRenderer();
        _inMemory = new InMemoryPdfRenderer();

        BenchmarkSupport.EnsureEquivalent("Contract/QuestPDF", QuestPdfRawDocuments.Contract(_contract), Render(_quest));
        BenchmarkSupport.EnsureEquivalent("Contract/iText", new ITextRawDocuments().Contract(_contract), Render(_itext));
        _ = Render(_inMemory);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("QuestPDF")]
    public byte[] QuestPdf_Raw() => QuestPdfRawDocuments.Contract(_contract);

    [Benchmark]
    [BenchmarkCategory("QuestPDF")]
    public RenderedPdf QuestPdf_FluentPdf() => Render(_quest);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("iText")]
    public byte[] IText_Raw() => new ITextRawDocuments().Contract(_contract);

    [Benchmark]
    [BenchmarkCategory("iText")]
    public RenderedPdf IText_FluentPdf() => Render(_itext);

    [Benchmark]
    [BenchmarkCategory("Pipeline")]
    public RenderedPdf Pipeline_InMemory() => Render(_inMemory);

    private RenderedPdf Render(IPdfRenderer renderer) =>
        new RenderDocumentUseCase(renderer).Execute(_template.Build(_contract).Value).Value;
}
