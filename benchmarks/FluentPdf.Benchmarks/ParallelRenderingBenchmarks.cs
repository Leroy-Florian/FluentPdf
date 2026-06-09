using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using FluentPdf.Adapters.IText;
using FluentPdf.Adapters.QuestPdf;
using FluentPdf.Application.Rendering;
using FluentPdf.Application.UseCases;
using FluentPdf.Infrastructure.Rendering;
using FluentPdf.Samples.Invoice;

namespace FluentPdf.Benchmarks;

/// <summary>
/// Mass rendering: render a batch of documents through a single shared renderer, sequentially
/// and in parallel across all cores. The parallel/sequential time ratio shows throughput scaling
/// (a thread-safe pipeline should approach 1/cores), and the per-batch `Allocated` column should
/// be the same in both modes — proving parallelism adds no retained state or memory blow-up.
/// </summary>
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class ParallelRenderingBenchmarks
{
    /// <summary>Documents rendered per batch.</summary>
    [Params(64)]
    public int Batch { get; set; }

    private readonly InvoiceDto _invoice = InvoiceSample.SampleData();
    private readonly InvoiceTemplate _template = new();
    private IPdfRenderer _quest = null!;
    private IPdfRenderer _itext = null!;

    [GlobalSetup]
    public void Setup()
    {
        _quest = new QuestPdfRenderer();
        _itext = new ITextRenderer();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("QuestPDF")]
    public void QuestPdf_Sequential() => RenderSequential(_quest);

    [Benchmark]
    [BenchmarkCategory("QuestPDF")]
    public void QuestPdf_Parallel() => RenderParallel(_quest);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("iText")]
    public void IText_Sequential() => RenderSequential(_itext);

    [Benchmark]
    [BenchmarkCategory("iText")]
    public void IText_Parallel() => RenderParallel(_itext);

    private void RenderSequential(IPdfRenderer renderer)
    {
        for (var i = 0; i < Batch; i++)
        {
            Render(renderer);
        }
    }

    private void RenderParallel(IPdfRenderer renderer) =>
        Parallel.For(0, Batch, _ => Render(renderer));

    private void Render(IPdfRenderer renderer) =>
        _ = new RenderDocumentUseCase(renderer).Execute(_template.Build(_invoice).Value).Value;
}
