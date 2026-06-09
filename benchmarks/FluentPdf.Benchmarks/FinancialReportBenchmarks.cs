using BenchmarkDotNet.Attributes;
using FluentPdf.Adapters.IText;
using FluentPdf.Adapters.QuestPdf;
using FluentPdf.Application.Rendering;
using FluentPdf.Application.UseCases;
using FluentPdf.Infrastructure.Rendering;
using FluentPdf.Samples.FinancialReport;

namespace FluentPdf.Benchmarks;

/// <summary>
/// The complex, chart-heavy report: KPI scorecards, three financial statements, a landscape
/// section and SkiaSharp-rendered bar/line/pie charts. It is benchmarked through the FluentPdf
/// pipeline on all three adapters to compare the libraries on a realistic, mixed-content
/// document and to show how much of the cost is the no-library pipeline itself (InMemory).
/// </summary>
/// <remarks>
/// There is intentionally no hand-written raw twin here. The cost is dominated by Skia chart
/// rasterisation, which both paths share identically through <c>SkiaChartRenderer</c>, so a raw
/// twin would mostly re-measure Skia while adding a large, fragile, easy-to-diverge reproduction
/// of the whole report. The Invoice and Contract benchmarks already isolate the pipeline tax.
/// </remarks>
[MemoryDiagnoser]
public class FinancialReportBenchmarks
{
    private readonly FinancialReportDto _report = FinancialReportSample.SampleData();
    private readonly FinancialReportTemplate _template = new();
    private IPdfRenderer _quest = null!;
    private IPdfRenderer _itext = null!;
    private IPdfRenderer _inMemory = null!;

    [GlobalSetup]
    public void Setup()
    {
        _quest = new QuestPdfRenderer();
        _itext = new ITextRenderer();
        _inMemory = new InMemoryPdfRenderer();

        // Fail fast if any adapter cannot render the report (e.g. a missing capability).
        _ = Render(_quest);
        _ = Render(_itext);
        _ = Render(_inMemory);
    }

    [Benchmark]
    public RenderedPdf QuestPdf() => Render(_quest);

    [Benchmark]
    public RenderedPdf IText() => Render(_itext);

    [Benchmark(Baseline = true)]
    public RenderedPdf Pipeline_InMemory() => Render(_inMemory);

    private RenderedPdf Render(IPdfRenderer renderer) =>
        new RenderDocumentUseCase(renderer).Execute(_template.Build(_report).Value).Value;
}
