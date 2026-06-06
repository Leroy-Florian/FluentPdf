namespace FluentPdf.Domain;

/// <summary>
/// Document-level metadata written to the PDF information dictionary. Every field is
/// optional; an empty instance is valid.
/// </summary>
public sealed record DocumentMetadata
{
    /// <summary>The document title.</summary>
    public string? Title { get; init; }

    /// <summary>The document author.</summary>
    public string? Author { get; init; }

    /// <summary>The document subject.</summary>
    public string? Subject { get; init; }

    /// <summary>The application that created the source content.</summary>
    public string? Creator { get; init; }

    /// <summary>Free-form keywords describing the document.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = [];

    /// <summary>Metadata with no fields set.</summary>
    public static DocumentMetadata Empty { get; } = new();
}
