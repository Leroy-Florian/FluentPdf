using FluentPdf.Adapters.IText;
using FluentPdf.Adapters.QuestPdf;
using FluentPdf.Application.Rendering;
using FluentPdf.Charting.Skia;
using FluentPdf.Kernel;
using FluentPdf.Samples.Contract;
using FluentPdf.Samples.FinancialReport;
using FluentPdf.Samples.Invoice;
using FluentPdf.Visual;
using FluentPdf.VisualGate;

// FluentPdf visual regression gate.
//
//   dotnet run --project tools/FluentPdf.VisualGate -- check   [--baselines DIR] [--out DIR] [--threshold 0.999]
//   dotnet run --project tools/FluentPdf.VisualGate -- update  [--baselines DIR]
//
// "update" (re)writes the golden images; "check" re-renders every sample through every
// adapter and fails (exit 1) if any page drifts from its golden beyond the threshold. Run it
// in CI as a quality gate: a layout regression turns the build red and the diff images are
// written to the output directory for inspection.

var mode = args.Length > 0 ? args[0].ToLowerInvariant() : "check";
var baselines = ArgValue("--baselines") ?? "tests/VisualBaselines";
var outputDir = ArgValue("--out") ?? "artifacts/visual-diff";
var threshold = double.TryParse(ArgValue("--threshold"), System.Globalization.CultureInfo.InvariantCulture, out var t) ? t : 0.995d;
var metric = (ArgValue("--metric") ?? "ssim").ToLowerInvariant(); // "ssim" or "pixel"
var bandCount = int.TryParse(ArgValue("--bands"), out var bc) ? bc : 12;
var ignoredBands = ParseBands(ArgValue("--ignore-bands"));

var samples = new SampleDefinition[]
{
    new("invoice", InvoiceSample.Render, Pages: 1),
    new("finreport", FinancialReportSample.RenderFullReport, Pages: 3),
    new("contract", ContractSample.Render, Pages: 2),
};

// Inject the Skia chart renderer so the financial-report sample (which uses charts) renders.
var charts = new SkiaChartRenderer();
var adapters = new (string Name, IPdfRenderer Renderer)[]
{
    ("questpdf", new QuestPdfRenderer(charts)),
    ("itext", new ITextRenderer(charts)),
};

var rasterizer = new PdfRasterizer(700, 990);
var comparer = new PdfVisualComparer(rasterizer, colorTolerance: 48, bands: bandCount);

Directory.CreateDirectory(baselines);

if (mode == "update")
{
    return RunUpdate();
}

if (mode != "check")
{
    Console.Error.WriteLine($"Unknown mode '{mode}'. Use 'check' or 'update'.");
    return 2;
}

return RunCheck();

int RunUpdate()
{
    var written = 0;

    foreach (var sample in samples)
    {
        foreach (var (adapterName, renderer) in adapters)
        {
            var pages = RenderPages(sample, renderer, adapterName);
            for (var i = 0; i < pages.Count; i++)
            {
                File.WriteAllBytes(GoldenPath(sample.Name, adapterName, i + 1), GoldenImages.Encode(pages[i]));
                written++;
            }
        }
    }

    Console.WriteLine($"Updated {written} golden image(s) under '{baselines}'.");
    return 0;
}

int RunCheck()
{
    var failures = 0;
    var checkedPages = 0;

    foreach (var sample in samples)
    {
        foreach (var (adapterName, renderer) in adapters)
        {
            var pages = RenderPages(sample, renderer, adapterName);

            for (var i = 0; i < pages.Count; i++)
            {
                checkedPages++;
                var goldenPath = GoldenPath(sample.Name, adapterName, i + 1);
                var label = $"{sample.Name}/{adapterName} p{i + 1}";

                if (!File.Exists(goldenPath))
                {
                    Console.Error.WriteLine($"  MISSING  {label}  (no baseline — run 'update')");
                    failures++;
                    continue;
                }

                var golden = GoldenImages.Load(goldenPath);
                var result = comparer.ComparePages(i + 1, golden, pages[i]);
                var (score, worst) = Evaluate(result, metric, ignoredBands);

                if (score >= threshold)
                {
                    Console.WriteLine($"  OK       {label}  {metric}={score:F4}  (ssim={result.Ssim:F4} pixel={result.Similarity:F4})");
                }
                else
                {
                    failures++;
                    Directory.CreateDirectory(outputDir);
                    File.WriteAllBytes(Path.Combine(outputDir, $"{sample.Name}.{adapterName}.p{i + 1}.actual.png"), GoldenImages.Encode(pages[i]));
                    File.WriteAllBytes(Path.Combine(outputDir, $"{sample.Name}.{adapterName}.p{i + 1}.diff.png"), result.DiffPng);
                    var zone = worst is null ? "page" : $"band {worst.Index + 1}/{result.Bands.Count} (rows {worst.Top}-{worst.Bottom})";
                    Console.Error.WriteLine($"  DRIFT    {label}  {metric}={score:F4} < {threshold:F4} in {zone} → diff in {outputDir}");
                }
            }
        }
    }

    Console.WriteLine();
    if (failures == 0)
    {
        Console.WriteLine($"Visual gate PASSED — {checkedPages} page(s) match their baseline.");
        return 0;
    }

    Console.Error.WriteLine($"Visual gate FAILED — {failures} of {checkedPages} page(s) drifted. See '{outputDir}'.");
    return 1;
}

IReadOnlyList<RasterPage> RenderPages(SampleDefinition sample, IPdfRenderer renderer, string adapterName)
{
    var result = sample.Render(renderer);
    if (result.IsFailure)
    {
        throw new InvalidOperationException($"{sample.Name}/{adapterName} failed to render: {result.Error.Code} — {result.Error.Message}");
    }

    var pages = rasterizer.Rasterize(result.Value.ToArray());
    return [.. pages.Take(Math.Min(sample.Pages, pages.Count))];
}

string GoldenPath(string sample, string adapter, int page) =>
    Path.Combine(baselines, $"{sample}.{adapter}.p{page}.png");

string? ArgValue(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

// The page's score is the worst (minimum) score across the bands that are not ignored, so a
// localised regression in any kept band fails the gate — while volatile zones (a dated header,
// say) can be excluded with --ignore-bands.
static (double Score, BandComparison? Worst) Evaluate(PageComparison page, string metric, HashSet<int> ignored)
{
    var score = 1d;
    BandComparison? worst = null;

    foreach (var band in page.Bands)
    {
        if (ignored.Contains(band.Index))
        {
            continue;
        }

        var value = metric == "pixel" ? band.PixelSimilarity : band.Ssim;
        if (worst is null || value < score)
        {
            score = value;
            worst = band;
        }
    }

    return (score, worst);
}

static HashSet<int> ParseBands(string? csv)
{
    var set = new HashSet<int>();
    if (string.IsNullOrWhiteSpace(csv))
    {
        return set;
    }

    foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (int.TryParse(part, out var oneBased))
        {
            set.Add(oneBased - 1); // input is 1-based to match the "band N/M" report
        }
    }

    return set;
}
