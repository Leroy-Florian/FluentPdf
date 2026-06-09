# FluentPdf — Benchmarks

These benchmarks answer one question:

> **What does the agnostic layer cost?** FluentPdf lets you describe a document once and render
> it through any adapter, but it interposes a pipeline — build an immutable domain model, scan it
> for unsupported features, then translate it through the adapter — between your code and the PDF
> library. This project measures that overhead by rendering the **same document two ways**: with
> the library driven directly ("raw"), and through the FluentPdf pipeline.

Built on [BenchmarkDotNet](https://benchmarkdotnet.org/).

## What is compared

For each document the suite runs, per library, a **raw baseline** and the **FluentPdf pipeline**:

| Variant              | What it does                                                              |
|----------------------|--------------------------------------------------------------------------|
| `*_Raw` (baseline)   | Hand-written QuestPDF / iText producing an equivalent document directly.  |
| `*_FluentPdf`        | `Template.Build(dto)` → `RenderDocumentUseCase.Execute` through the adapter. |
| `Pipeline_InMemory`  | The same FluentPdf pipeline on the no-library `InMemoryPdfRenderer`.      |

The `*_Raw` vs `*_FluentPdf` ratio (per library) **is the abstraction tax**. `Pipeline_InMemory`
shows the cost of FluentPdf's own pipeline with the PDF library taken out of the picture.

The raw baselines live in [`Raw/`](Raw) and deliberately reuse the adapters' shared font and table
theme (`FluentPdf.Adapters.Shared`) so the two documents are visually equivalent — same fonts, page
sizes, borders, pagination and text. To keep the comparison honest, `[GlobalSetup]` renders both
paths once and **throws if their page counts differ** (`BenchmarkSupport.EnsureEquivalent`): a
faster-but-different baseline can never silently skew the numbers.

### Documents

| Benchmark                   | Document                | Why                                                |
|-----------------------------|-------------------------|----------------------------------------------------|
| `InvoiceBenchmarks`         | 1-page invoice          | Isolates the **fixed per-render** pipeline cost.   |
| `ContractBenchmarks`        | ~35-page agreement      | Cost that **scales with content** + pagination.    |
| `FinancialReportBenchmarks` | Chart-heavy report      | Realistic mixed content; **library comparison**.   |

`FinancialReportBenchmarks` has no raw twin: its cost is dominated by SkiaSharp chart
rasterisation, which both paths share identically through `SkiaChartRenderer`. A hand-written twin
would mostly re-measure Skia while adding a large, easy-to-diverge reproduction of the report — so
it is benchmarked through all three adapters instead, which is the more useful signal there.

## Running

```bash
# Everything (takes a while — the real-library renders are milliseconds each):
dotnet run -c Release --project benchmarks/FluentPdf.Benchmarks -- --filter '*'

# One document:
dotnet run -c Release --project benchmarks/FluentPdf.Benchmarks -- --filter '*Invoice*'

# A quick, lower-fidelity pass while iterating:
dotnet run -c Release --project benchmarks/FluentPdf.Benchmarks -- --filter '*' --job short
```

Results are written to `BenchmarkDotNet.Artifacts/`.

## Indicative results

Measured with `--job short` on a shared 4-core CI container (Intel Xeon 2.80 GHz, .NET 10) — the
**magnitudes and ratios** are what matter, not the absolute milliseconds; re-run them on your own
hardware. `Ratio`/`Alloc Ratio` are relative to the raw baseline of the same library.

**Invoice (1 page)**

| Method             | Mean       | Ratio | Allocated | Alloc Ratio |
|--------------------|-----------:|------:|----------:|------------:|
| QuestPdf_Raw       | 2,687 µs   | 1.00  | 140 KB    | 1.00        |
| QuestPdf_FluentPdf | 2,728 µs   | 1.02  | 260 KB    | 1.85        |
| IText_Raw          | 8,266 µs   | 1.00  | 3,256 KB  | 1.00        |
| IText_FluentPdf    | 8,439 µs   | 1.03  | 3,793 KB  | 1.16        |
| Pipeline_InMemory  | 13 µs      | —     | 18 KB     | —           |

**Contract (~35 pages)**

| Method             | Mean       | Ratio | Allocated | Alloc Ratio |
|--------------------|-----------:|------:|----------:|------------:|
| QuestPdf_Raw       | 111 ms     | 1.00  | 1.51 MB   | 1.00        |
| QuestPdf_FluentPdf | 115 ms     | 1.03  | 2.52 MB   | 1.66        |
| IText_Raw          | 147 ms     | 1.00  | 42.7 MB   | 1.00        |
| IText_FluentPdf    | 202 ms     | 1.37  | 43.6 MB   | 1.02        |
| Pipeline_InMemory  | 1.56 ms    | —     | 1.35 MB   | —           |

**Financial report (charts, mixed content)**

| Method            | Mean    | Allocated |
|-------------------|--------:|----------:|
| QuestPdf          | 178 ms  | 3.15 MB   |
| IText             | 269 ms  | 36.3 MB   |
| Pipeline_InMemory | 0.31 ms | 0.34 MB   |

### How to read this

- **The abstraction tax on time is small** — typically within a few percent and often inside the
  run-to-run noise. The pipeline (build the model + scan features + use case) is dwarfed by the PDF
  library's own work: `Pipeline_InMemory` renders in **microseconds** where the real libraries take
  **milliseconds to hundreds of milliseconds**.
- **The tax shows up mostly in allocations**, and mostly on cheap documents — the extra is the
  immutable domain tree plus the `RenderedPdf` byte copy. As the document grows, the library's own
  allocations dominate and the relative overhead shrinks (1.85× → 1.66× for QuestPDF; 1.16× → 1.02×
  for iText).
- **Your choice of library matters far more than the abstraction.** On the financial report iText
  allocates ~11× more than QuestPDF (36 MB vs 3 MB) and runs ~50% slower — exactly the kind of
  trade-off these benchmarks exist to surface, and exactly the swap FluentPdf makes a one-line
  change.

> The wide `Error`/`StdDev` on the shared container (especially for iText) means small time ratios
> should be read as "≈ parity", not precise figures. Re-run on a quiet machine for tighter numbers.
