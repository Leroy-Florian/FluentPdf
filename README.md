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
            .Column(4, c => c.Paragraph("Jane Doe", alignment: HorizontalAlignment.Right)))
        .Table(t => t
            .Columns(2)
            .HeaderRow(r => r.Cell("Description").Cell("Total"))
            .Row(r => r.Cell("Widget").Cell("19.98"))))
    .Build();                                          // Result<PdfDocument>
```

**Reusable blocks** are first-class. A component maps a print DTO to blocks and can be reused
across documents:

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

See [`samples/FluentPdf.Samples`](samples/FluentPdf.Samples) for the full, compiling invoice
example.

## Projects

| Project | Role |
|---------|------|
| `FluentPdf.Kernel` | Shared DDD building blocks: `Result<T>`, `Error`, `ValueObject`, `Entity`, `AggregateRoot`. |
| `FluentPdf.Domain` | The library-agnostic document model (aggregate, value objects, content elements). |
| `FluentPdf.Application` | The fluent builder, reusable components/templates, the `IPdfRenderer` port and the rendering use case. **The package consumers reference.** |
| `FluentPdf.Infrastructure` | Built-in adapters, incl. the `InMemoryPdfRenderer` reference implementation. |
| `FluentPdf.Conformance` | The shared contract-test kit every adapter must pass. |

Layering (enforced by `FluentPdf.ArchitectureTests`):

```
Kernel  ←  Domain  ←  Application  ←  Infrastructure
```

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
