using FluentPdf.Application.Rendering;
using FluentPdf.Kernel;

namespace FluentPdf.VisualGate;

/// <summary>A sample to gate: its name, how to render it, and how many leading pages to check.</summary>
internal sealed record SampleDefinition(
    string Name,
    Func<IPdfRenderer, Result<RenderedPdf>> Render,
    int Pages);
