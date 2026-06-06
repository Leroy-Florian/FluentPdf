# FluentPdf — Architecture & Development Rules

FluentPdf is a **library-agnostic fluent builder for PDF documents**. The conventions and
quality bar are inherited from the [WOW](https://github.com/Leroy-Florian/WOW) project.

## Core mission

Let callers describe a document with an expressive fluent API and reusable blocks, and render
it through a pluggable adapter — **without the core depending on any PDF library**. The
agnostic document model is the contract; adapters translate it.

## Hexagonal / Clean architecture (strict)

```
Domain        → Kernel only
Application   → Domain + Kernel
Infrastructure→ Application + Domain + Kernel
```

**Violations are fatal.** `FluentPdf.ArchitectureTests` (NetArchTest) enforces layering and
naming automatically.

- **Kernel** — DDD building blocks only (`Result<T>`, `Error`, `ValueObject`, `Entity`,
  `AggregateRoot`, `IDomainEvent`). No dependencies.
- **Domain** — the immutable, agnostic document model. No framework refs, no async, no
  ORM/IO, no PDF library. All invariants enforced in factories; all concrete classes
  `sealed`; strongly-typed IDs; domain events are immutable records.
- **Application** — the fluent builder, reusable `IBlockComponent`/`IDocumentTemplate`, the
  `IPdfRenderer` **port**, capability model and the rendering use case. No infrastructure
  dependencies.
- **Infrastructure** — adapters (driven side). The `InMemoryPdfRenderer` is the reference
  adapter and depends on no third-party library.
- **Conformance** — the shared contract-test kit every adapter must pass.

## Library-agnosticism rules

- Adapters integrate a PDF library by implementing `IPdfRenderer` in their own package; the
  core never references a PDF library.
- An adapter **must** declare its `RendererCapabilities` accurately and return a failed
  `Result` (never throw) for content it cannot render.
- Unsupported content is detected up front by `DocumentFeatureScanner` +
  `RenderDocumentUseCase`, never silently dropped.
- Every adapter **must** pass `PdfRendererContractTests`. Conformance asserts *semantic*
  equivalence (PDF header, text presence/order, page count, capability honouring) — never
  pixel/byte equality.

## Error handling

- Business failures use `Result` / `Result<T>` + `Error`, **never exceptions**.
- Exceptions are reserved for programmer errors (e.g. null injected dependencies).

## Code style

- Primary constructors for non-domain classes where there is no init logic.
- Collection expressions (`[...]`) are mandatory (enforced as errors via `.editorconfig`).
- `TreatWarningsAsErrors` is on; builds must be warning-clean.
- Value objects derive from `ValueObject` and compare by component.

## Testing (non-negotiable)

- xUnit + FluentAssertions. **No mocking frameworks** — hand-written fakes only.
- Unit tests are fast and sociable (real domain, fake infra).
- Architecture tests are mandatory (NetArchTest).
- Mutation testing via Stryker (`stryker-config.json`); aim high on Domain + Application.
- A new feature is not done until it is covered, including error paths.

## Project layout

```
src/
  FluentPdf.Kernel/          # DDD building blocks
  FluentPdf.Domain/          # agnostic document model
  FluentPdf.Application/     # fluent builder, components, templates, port, use case
  FluentPdf.Infrastructure/  # adapters (InMemoryPdfRenderer)
  FluentPdf.Conformance/     # adapter contract-test kit
tests/                       # unit + architecture + conformance-runner tests
samples/FluentPdf.Samples/   # worked invoice example
```

## Technology

- .NET 10 SDK / C# latest; libraries multi-target `netstandard2.0;net8.0;net10.0`.
- Centralized package versions in `Directory.Packages.props`.
- xUnit, FluentAssertions, coverlet, NetArchTest, Stryker.NET.
