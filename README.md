# FluentPdf

A **library-agnostic, fluent builder for PDF documents**, built with Domain-Driven Design
and a strict hexagonal (ports & adapters) architecture. You describe *what* a document
contains through an expressive fluent API and reusable blocks; a pluggable renderer adapter
decides *how* to turn that description into bytes — using QuestPDF, iText, PDFsharp or any
other library, without the core ever depending on it.

> Engineering conventions, architecture and the quality bar are inherited from the
> [WOW](https://github.com/Leroy-Florian/WOW) project: DDD, hexagonal layering enforced by
> architecture tests, `Result<T>` instead of business exceptions, centralized package
> versions, unit tests and mutation testing (Stryker).

## Why "agnostic"?

The heart of FluentPdf is an **immutable document model** (`FluentPdf.Domain`). It is the
contract. Rendering happens behind a single driven port:

```csharp
public interface IPdfRenderer
{
    RendererDescriptor Descriptor { get; }      // name + declared capabilities
    Result<RenderedPdf> Render(PdfDocument document);
}
```

Each PDF library is integrated by implementing `IPdfRenderer` in its own adapter package.
The core never references any third-party PDF library.

## How do we prove it works with iText, QuestPDF, … and handle their differences?

Different libraries support different features and lay content out differently. FluentPdf
turns that from a risk into a guarantee with three mechanisms:

1. **Negotiated capabilities.** An adapter declares what it supports via
   `RendererCapabilities`. Before rendering, `DocumentFeatureScanner` computes the features a
   document actually uses and `RenderDocumentUseCase` fails fast with a *named* error if the
   adapter can't render one of them. Unsupported content is never silently dropped or
   misrendered.

2. **A shared adapter conformance kit** (`FluentPdf.Conformance`). An abstract xUnit suite,
   `PdfRendererContractTests`, that *every* adapter must pass. Writing an adapter means
   implementing `IPdfRenderer` and adding a ~5-line test subclass:

   ```csharp
   public sealed class ITextRendererConformanceTests : PdfRendererContractTests
   {
       protected override IPdfRenderer CreateRenderer() => new ITextRenderer();
       protected override string ExtractText(IReadOnlyList<byte> content) =>
           /* extract text with iText's parser */;
   }
   ```

   The built-in `InMemoryPdfRenderer` passes the very same kit, which proves the kit and the
   model are sound.

3. **Semantic, not pixel, equivalence.** The kit asserts on invariants every library must
   honour — a valid `%PDF-` header, the presence and ordering of text, page counts, capability
   honouring — never byte- or pixel-level equality. That is what keeps one document model
   portable across renderers.

## The fluent builder, reusable blocks and a Bootstrap-style grid

```csharp
var result = PdfDocumentBuilder.Create()
    .Metadata(m => m.Title("Invoice INV-001").Author("ACME"))
    .Section(s => s
        .Margins(48)
        .Header(h => h.Paragraph("ACME Corp"))
        .Footer(f => f.Paragraph("Thank you"))
        .Row(r => r                                   // 12-unit grid, à la Bootstrap
            .Column(8, c => c.Paragraph(p => p.Bold("Invoice INV-001")))
            .Column(c => c.Paragraph("Jane Doe", alignment: HorizontalAlignment.Right))) // auto: shares the free space
        .Table(t => t
            .Columns(2)
            .HeaderRow(r => r.Cell("Description").Cell("Total"))
            .Row(r => r.Cell("Widget").Cell("19.98")))
        .Chart(c => c                                  // agnostic chart: data, not pixels
            .Line()
            .Title("Revenue trend")
            .Categories("Q1", "Q2", "Q3", "Q4")
            .Series("Revenue", 441_200, 458_700, 469_900, 482_300)))
    .Build();                                          // Result<PdfDocument>
```

Grid columns are either explicit (`Column(width, …)`, 1-12) or **auto** (`Column(…)`), in
which case they share whatever space the explicit columns leave free — like Bootstrap's
`col` vs `col-N`.

**Charts are agnostic too**: `Chart(…)` describes a `Bar`/`Line`/`Pie` with categories and
named numeric series — *what* to plot, never how it is painted. Adapters draw it with their
own charting library; an adapter that can't declares the `Chart` capability unsupported, so
the use case fails fast instead of dropping it silently.

### Automatic pagination (built for mass printing)

Long documents (a 30-40 page contract) flow across pages automatically. Because the core
depends on no PDF library, it cannot measure glyphs itself — so measurement is a **port**,
`ITextMeasurer`, supplied by the adapter; a dependency-free `ApproximateTextMeasurer` is the
built-in default. The `DocumentPaginator` then:

- fills pages top-to-bottom, **splitting long paragraphs at word boundaries**, **tables at
  row boundaries** (repeating header rows), **lists at item boundaries** (ordered numbering
  stays continuous across the break) and **grid rows column-by-column**, honouring explicit
  page breaks;
- **streams pages lazily** (`IEnumerable` + `yield`) so memory stays flat — a 40-page
  agreement or a mass-print run of thousands never holds more than one page at a time;
- resolves **page-number fields** (`PageNumber("Page {page} of {pages}")`) once the totals
  are known — the total page count is computed up front only when a field references it.

Measurement runs on `ReadOnlySpan<char>` (no per-word allocations on the hot path), keeping
the time/RAM overhead minimal for high-volume workloads.

```csharp
.Footer(f => f.PageNumber("Page {page} of {pages}", HorizontalAlignment.Center))
```

**Reusable blocks** are first-class — as interfaces (`IBlockComponent` /
`IBlockComponent<TModel>`) for testable, injectable components, or as inline delegates
(`Component(dto, d => …)`) for quick cases. A component maps a print DTO to blocks and can
be reused across documents:

```csharp
public sealed class InvoiceHeaderComponent : IBlockComponent<InvoiceDto>
{
    public Result<IReadOnlyList<IBlock>> Build(InvoiceDto invoice) =>
        BlockComposer.Compose(b => b
            .Row(r => r
                .Column(8, c => c.Paragraph(p => p.Bold($"Invoice {invoice.Number}")))
                .Column(4, c => c.Paragraph(invoice.CustomerName)))
            .Spacer(12));
}
```

A **template** assembles a whole document from a DTO by composing components:

```csharp
public sealed class InvoiceTemplate : IDocumentTemplate<InvoiceDto>
{
    private readonly InvoiceHeaderComponent _header = new();
    private readonly InvoiceLinesComponent _lines = new();

    public Result<PdfDocument> Build(InvoiceDto invoice) =>
        PdfDocumentBuilder.Create()
            .Metadata(m => m.Title($"Invoice {invoice.Number}"))
            .Section(s => s.Component(_header, invoice).Component(_lines, invoice))
            .Build();
}
```

See [`samples/FluentPdf.Samples`](samples/FluentPdf.Samples) for the full, compiling examples:

- **Invoice** (`Invoice/`) — the minimal end-to-end walkthrough.
- **Financial report** (`FinancialReport/`) — a deliberately complex, multi-section quarterly
  report (cover, KPI scorecard, trend line chart, three financial statements, segment
  breakdown with a revenue-mix pie chart, risk register, sign-off) decomposed into small
  **reusable business blocks**. The keystone
  `FinancialStatementComponent` is fed the income statement, balance sheet and cash-flow
  statement in turn, and a separate `BoardOnePagerTemplate` re-composes the very same blocks
  into a one-page briefing — demonstrating blocks that are authored once and reused across
  documents.
- **Contract** (`Contract/`) — a 30-40 page senior facility agreement with real long-form
  legal prose, exercising **automatic pagination** (paragraph splitting), a running header
  and a "Page X of Y" footer resolved during layout.

## Projects

| Project | Role |
|---------|------|
| `FluentPdf.Kernel` | Shared DDD building blocks: `Result<T>`, `Error`, `ValueObject`, `Entity`, `AggregateRoot`. |
| `FluentPdf.Domain` | The library-agnostic document model (aggregate, value objects, content elements). |
| `FluentPdf.Application` | The fluent builder, reusable components/templates, the `IPdfRenderer` and `ITextMeasurer` ports, the streaming `DocumentPaginator` and the rendering use case. **The package consumers reference.** |
| `FluentPdf.Infrastructure` | Built-in adapters, incl. the `InMemoryPdfRenderer` reference implementation. |
| `FluentPdf.Conformance` | The shared contract-test kit every adapter must pass. |
| `FluentPdf.Adapters.Shared` | Shared adapter support — embedded **Liberation Sans** font and the `RenderingTheme` (table borders, header shading, palette). **Pure managed, no native deps.** |
| `FluentPdf.Adapters.QuestPdf` | Real adapter backed by **QuestPDF**. Maps the model onto QuestPDF's layout; page-number fields use QuestPDF's native counters. |
| `FluentPdf.Adapters.iText` | Real adapter backed by **iText 7**. Maps the model onto iText elements; headers/footers and "Page X of Y" are drawn once the page count is known. |
| `FluentPdf.Adapters.PdfSharp` | Real adapter backed by **PDFsharp/MigraDoc** (MIT, **fully managed — no native deps**). Maps the model onto MigraDoc's document-flow model; MigraDoc paginates and resolves running headers/footers and page-number fields. |
| `FluentPdf.Charting.Skia` | **Optional** SkiaSharp `IChartRenderer` (chart → PNG). Reference it only to render charts; this is the *only* package that pulls SkiaSharp. |
| `FluentPdf.Visual` | A PDF visual-comparison engine (PDFium rasterisation): per-page similarity / SSIM scoring and red diff heatmaps. **Tooling only — never shipped to consumers.** |

All three real adapters pass the same `PdfRendererContractTests` as the reference adapter, and
share a deliberate design so their output is **visually consistent** (same embedded font ⇒
identical glyph metrics; same table styling from `RenderingTheme`).

**Charts are opt-in.** An adapter renders charts only when given an `IChartRenderer`
(`new QuestPdfRenderer(new SkiaChartRenderer())`); without one it declares the `Chart`
capability unsupported and the use case refuses chart content. So **SkiaSharp and its native
assets stay out of the default footprint** — they arrive only if you reference
`FluentPdf.Charting.Skia`. The chart is drawn once there, so a bar/line/pie looks identical in
the QuestPDF and iText output. (PDFium, used by `FluentPdf.Visual`, is tooling-only and never
reaches a consumer.)

### Visual verification

Two layout engines never produce byte-identical pixels (sub-pixel positioning and line-break
algorithms differ), so the visual engine is for **regression** — compare an adapter's output
to a stored golden, identical ⇒ similarity `1.0` — and for **human review** via a red diff
heatmap. The adapter integration tests render the real samples (invoice, financial report
with charts, 30-page contract) through QuestPDF and iText, confirm each is a valid, non-blank
PDF whose text is present, and produce a diff for inspection.

### CI quality gate

`tools/FluentPdf.VisualGate` turns the engine into a release gate. It renders every sample
through every adapter, flattens each page over white, and compares it to a committed golden
baseline (`tests/VisualBaselines/`). Because an adapter is compared **to itself**, rendering
is deterministic and a clean run scores `1.0000` on every page; any layout change drifts a
page below the threshold and fails the build, writing the offending page and a red diff to
`artifacts/visual-diff/`.

It scores each page with two metrics: a raw **pixel** similarity and a perceptual **SSIM**
(structural similarity, the default — robust to imperceptible noise, sensitive to real layout
shifts). Every page is also split into horizontal **bands**: the gate reports *which* band
drifted (e.g. `band 3/12 (rows 166-249)`), and volatile zones (a dated header/footer) can be
excluded with `--ignore-bands`. The page's score is the worst kept band, so a localised
regression still fails the gate.

```bash
# Re-generate the baselines (run once in your CI image, then commit them):
dotnet run --project tools/FluentPdf.VisualGate -c Release -- update

# Gate a release / PR (exit code 1 on any visual drift):
dotnet run --project tools/FluentPdf.VisualGate -c Release -- check \
    --metric ssim --threshold 0.99 --bands 12 --ignore-bands 1,12
```

The [`visual-gate`](.github/workflows/visual-gate.yml) GitHub Actions workflow runs `check` on
every push, PR and published release, and uploads the diff images when it fails. Baselines are
environment-sensitive (font stack, library versions): regenerate them with `update` whenever
the CI image or the QuestPDF/iText/SkiaSharp versions change.

### NuGet packages

The library ships as a small family of packages, but a consumer installs **one**: the adapter
they want. Everything else is pulled in transitively — and the core never drags a PDF library.

| Package | Pulls in | Install it when… |
|---------|----------|------------------|
| `FluentPdf` | `FluentPdf.Domain`, `FluentPdf.Kernel` | you build your own adapter |
| `FluentPdf.Domain` / `FluentPdf.Kernel` | — | (transitive; never installed directly) |
| `FluentPdf.Infrastructure` | `FluentPdf` | you want the in-memory reference renderer |
| `FluentPdf.Conformance` | `FluentPdf`, xUnit | you author an adapter and want the contract tests |
| `FluentPdf.Adapters.Shared` | `FluentPdf` (fonts/theme, no native) | (transitive via an adapter) |
| **`FluentPdf.Adapters.QuestPdf`** | `…Shared`, QuestPDF | **you render with QuestPDF** |
| **`FluentPdf.Adapters.iText`** | `…Shared`, iText 7 | **you render with iText** |
| **`FluentPdf.Adapters.PdfSharp`** | `…Shared`, PDFsharp/MigraDoc | **you render with PDFsharp (MIT, no native deps)** |
| `FluentPdf.Charting.Skia` | `FluentPdf`, `…Shared`, SkiaSharp | **only if you need charts** |

```bash
dotnet add package FluentPdf.Adapters.QuestPdf      # → FluentPdf + Shared + QuestPDF, no SkiaSharp
dotnet add package FluentPdf.Charting.Skia          # add only if you render charts
```

So **your core has no transitive dependency on iText or QuestPDF** — only the adapter you pick
brings its library (the QuestPDF adapter never pulls iText, and vice-versa), and **SkiaSharp
arrives only via the optional charting package**. `dotnet pack
FluentPdf.slnx` builds the whole family; the [`release`](.github/workflows/release.yml)
workflow packs and pushes them to NuGet on a published release.

Layering (enforced by `FluentPdf.ArchitectureTests`):

```
Kernel  ←  Domain  ←  Application  ←  Infrastructure
                            ↖  Adapters (QuestPDF, iText)
```

Adapters depend only on the `Application`/`Domain`/`Kernel` contracts — never the other way
round — so the core stays free of any PDF library.

## Target frameworks

The published libraries multi-target `netstandard2.0;net8.0;net10.0`, so they are consumable
across the whole modern .NET range (.NET 5 → 10 and beyond).

## Building and testing

```bash
dotnet build FluentPdf.slnx
dotnet test  FluentPdf.slnx
dotnet tool restore && dotnet stryker   # mutation testing
```

## License

MIT — see [LICENSE](LICENSE).
