using FluentPdf.Domain;
using FluentPdf.Domain.Content;

namespace FluentPdf.Application.Layout;

/// <summary>
/// One page produced by the <see cref="DocumentPaginator"/>: its 1-based global number, the
/// owning <see cref="Section"/> (for page size, margins and geometry) and the block content
/// placed on it. Header and footer blocks have their page-number fields already resolved, so
/// consumers can render them verbatim. Pages are streamed, so only one is alive at a time.
/// </summary>
public sealed class LaidOutPage
{
    internal LaidOutPage(
        int number,
        Section section,
        IReadOnlyList<IBlock> header,
        IReadOnlyList<IBlock> body,
        IReadOnlyList<IBlock> footer)
    {
        Number = number;
        Section = section;
        Header = header;
        Body = body;
        Footer = footer;
    }

    /// <summary>The 1-based page number across the whole document.</summary>
    public int Number { get; }

    /// <summary>The section this page belongs to.</summary>
    public Section Section { get; }

    /// <summary>The resolved running-header blocks (empty when the section has no header).</summary>
    public IReadOnlyList<IBlock> Header { get; }

    /// <summary>The body blocks (possibly fragments of split paragraphs/tables) on this page.</summary>
    public IReadOnlyList<IBlock> Body { get; }

    /// <summary>The resolved running-footer blocks (empty when the section has no footer).</summary>
    public IReadOnlyList<IBlock> Footer { get; }
}
