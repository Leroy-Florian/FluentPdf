using FluentPdf.Adapters.IText;
using FluentPdf.Adapters.QuestPdf;
using FluentPdf.Application.Rendering;
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
var threshold = double.TryParse(ArgValue("--threshold"), System.Globalization.CultureInfo.InvariantCulture, out var t) ? t : 0.999d;

var samples = new SampleDefinition[]
{
    new("invoice", InvoiceSample.Render, Pages: 1),
    new("finreport", FinancialReportSample.RenderFullReport, Pages: 3),
    new("contract", ContractSample.Render, Pages: 2),
};

var adapters = new (string Name, IPdfRenderer Renderer)[]
{
    ("questpdf", new QuestPdfRenderer()),
    ("itext", new ITextRenderer()),
};

var rasterizer = new PdfRasterizer(700, 990);
var comparer = new PdfVisualComparer();

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

                if (result.Similarity >= threshold)
                {
                    Console.WriteLine($"  OK       {label}  similarity={result.Similarity:F4}");
                }
                else
                {
                    failures++;
                    Directory.CreateDirectory(outputDir);
                    File.WriteAllBytes(Path.Combine(outputDir, $"{sample.Name}.{adapterName}.p{i + 1}.actual.png"), GoldenImages.Encode(pages[i]));
                    File.WriteAllBytes(Path.Combine(outputDir, $"{sample.Name}.{adapterName}.p{i + 1}.diff.png"), result.DiffPng);
                    Console.Error.WriteLine($"  DRIFT    {label}  similarity={result.Similarity:F4} < {threshold:F4}  ({result.DifferentPixels} px) → diff in {outputDir}");
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
