namespace FluentPdf.Domain.Content;

/// <summary>
/// Marker for block-level content (paragraphs, images, tables, lists, spacers, page
/// breaks). Blocks are immutable and stacked vertically within a section.
/// </summary>
public interface IBlock
{
}
