namespace FluentPdf.Application.Rendering;

/// <summary>
/// Identifies a concrete renderer adapter and advertises what it can render. Used in logs,
/// diagnostics and the unsupported-feature error message so failures name the adapter.
/// </summary>
/// <param name="Name">A human-readable adapter name, e.g. <c>"QuestPDF"</c> or <c>"iText7"</c>.</param>
/// <param name="Capabilities">The features the adapter supports.</param>
public sealed record RendererDescriptor(string Name, RendererCapabilities Capabilities);
