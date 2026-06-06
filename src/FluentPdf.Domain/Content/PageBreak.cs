namespace FluentPdf.Domain.Content;

/// <summary>
/// Forces subsequent content onto a new page. Stateless, so a single shared
/// <see cref="Instance"/> is reused.
/// </summary>
public sealed class PageBreak : IBlock
{
    private PageBreak()
    {
    }

    /// <summary>The shared page-break instance.</summary>
    public static PageBreak Instance { get; } = new();
}
