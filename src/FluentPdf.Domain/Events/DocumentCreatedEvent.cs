using FluentPdf.Kernel;

namespace FluentPdf.Domain.Events;

/// <summary>Raised when a <see cref="PdfDocument"/> is successfully assembled.</summary>
/// <param name="DocumentId">The identifier of the created document.</param>
/// <param name="SectionCount">The number of sections the document contains.</param>
public sealed record DocumentCreatedEvent(DocumentId DocumentId, int SectionCount) : IDomainEvent;
