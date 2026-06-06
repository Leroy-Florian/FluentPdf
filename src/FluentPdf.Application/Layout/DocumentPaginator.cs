using FluentPdf.Domain;
using FluentPdf.Domain.Content;
using FluentPdf.Domain.Styling;

namespace FluentPdf.Application.Layout;

/// <summary>
/// Lays a <see cref="PdfDocument"/> out into pages, measuring content through an injected
/// <see cref="ITextMeasurer"/>. Pages are produced lazily (<c>yield</c>), so a 40-page
/// contract — or a mass-print run of thousands — never holds more than one page's worth of
/// block references in memory.
/// </summary>
/// <remarks>
/// The engine fills pages top to bottom, honouring explicit page breaks, splitting long
/// paragraphs at word boundaries and long tables at row boundaries (repeating header rows),
/// and treating other blocks as atomic (moved whole to the next page, or placed alone when
/// taller than a page). Page-number fields are resolved as pages are emitted; the total page
/// count is computed up front only when a field actually references it.
/// </remarks>
public sealed class DocumentPaginator
{
    private static readonly IReadOnlyList<IBlock> NoBlocks = [];

    private readonly ITextMeasurer _measurer;

    public DocumentPaginator(ITextMeasurer measurer) =>
        _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));

    /// <summary>Streams the document's pages, resolving page-number fields as it goes.</summary>
    public IEnumerable<LaidOutPage> Paginate(PdfDocument document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var total = RequiresTotalPages(document) ? CountPages(document) : 0;
        var pageNumber = 0;

        foreach (var section in document.Sections)
        {
            var geometry = ComputeGeometry(section);

            foreach (var body in FillSection(section, geometry))
            {
                pageNumber++;
                yield return new LaidOutPage(
                    pageNumber,
                    section,
                    ResolveFields(section.Header?.Blocks ?? NoBlocks, pageNumber, total),
                    ResolveFields(body, pageNumber, total),
                    ResolveFields(section.Footer?.Blocks ?? NoBlocks, pageNumber, total));
            }
        }
    }

    /// <summary>Counts the pages the document lays out into, without retaining any of them.</summary>
    public int CountPages(PdfDocument document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var count = 0;

        foreach (var section in document.Sections)
        {
            var geometry = ComputeGeometry(section);

            foreach (var _ in FillSection(section, geometry))
            {
                count++;
            }
        }

        return count;
    }

    private IEnumerable<List<IBlock>> FillSection(Section section, Geometry geometry)
    {
        var page = new List<IBlock>();
        var remaining = geometry.ContentHeight;

        foreach (var block in section.Blocks)
        {
            if (block is PageBreak)
            {
                yield return page;
                page = [];
                remaining = geometry.ContentHeight;
                continue;
            }

            var current = block;

            while (current is not null)
            {
                var height = MeasureBlock(current, geometry.ContentWidth);

                if (height <= remaining)
                {
                    page.Add(current);
                    remaining -= height;
                    current = null;
                    continue;
                }

                var split = TrySplit(current, geometry.ContentWidth, remaining);

                if (split is { } parts)
                {
                    page.Add(parts.Head);
                    yield return page;
                    page = [];
                    remaining = geometry.ContentHeight;
                    current = parts.Tail;
                }
                else if (page.Count > 0)
                {
                    // Cannot split and the page already has content: retry on a fresh page.
                    yield return page;
                    page = [];
                    remaining = geometry.ContentHeight;
                }
                else
                {
                    // Empty page and the block neither fits nor splits: place it alone.
                    page.Add(current);
                    current = null;
                }
            }
        }

        // Flush the final page even when empty, so trailing breaks keep their semantics.
        yield return page;
    }

    private (IBlock Head, IBlock Tail)? TrySplit(IBlock block, double width, double maxHeight) =>
        block switch
        {
            Paragraph paragraph when paragraph.Runs.Count == 1 =>
                TrySplitParagraph(paragraph, width, maxHeight),
            TableBlock table => TrySplitTable(table, width, maxHeight),
            _ => null,
        };

    private (IBlock Head, IBlock Tail)? TrySplitParagraph(Paragraph paragraph, double width, double maxHeight)
    {
        var run = paragraph.Runs[0];
        var style = run.Style;
        var lineHeight = _measurer.LineHeight(style);
        var maxLines = (int)(maxHeight / lineHeight);

        if (maxLines < 1)
        {
            return null;
        }

        var text = run.Text;
        var span = text.AsSpan();
        var space = _measurer.SpaceWidth(style);

        var linesUsed = 1;
        var lineWidth = 0d;
        var placedAny = false;
        var lastWordEnd = 0;
        var headEnd = -1;
        var tailStart = -1;

        var pos = 0;
        while (pos < span.Length)
        {
            while (pos < span.Length && span[pos] == ' ')
            {
                pos++;
            }

            if (pos >= span.Length)
            {
                break;
            }

            var wordStart = pos;
            while (pos < span.Length && span[pos] != ' ')
            {
                pos++;
            }

            var wordWidth = _measurer.MeasureWord(span.Slice(wordStart, pos - wordStart), style);

            if (!placedAny)
            {
                lineWidth = wordWidth;
                placedAny = true;
            }
            else if (lineWidth + space + wordWidth <= width)
            {
                lineWidth += space + wordWidth;
            }
            else if (linesUsed >= maxLines)
            {
                headEnd = lastWordEnd;
                tailStart = wordStart;
                break;
            }
            else
            {
                linesUsed++;
                lineWidth = wordWidth;
            }

            lastWordEnd = pos;
        }

        if (headEnd <= 0 || tailStart < 0)
        {
            return null;
        }

        var head = Paragraph.FromText(text.Substring(0, headEnd), style, paragraph.Alignment).Value;
        var tail = Paragraph.FromText(text.Substring(tailStart), style, paragraph.Alignment).Value;
        return (head, tail);
    }

    private (IBlock Head, IBlock Tail)? TrySplitTable(TableBlock table, double width, double maxHeight)
    {
        var columnWidth = width / table.ColumnCount;

        List<TableRow> headerRows = [];
        List<TableRow> bodyRows = [];

        foreach (var row in table.Rows)
        {
            (row.IsHeader ? headerRows : bodyRows).Add(row);
        }

        if (bodyRows.Count < 2)
        {
            return null;
        }

        var used = 0d;
        foreach (var header in headerRows)
        {
            used += RowHeight(header, columnWidth);
        }

        var fit = 0;
        foreach (var bodyRow in bodyRows)
        {
            var rowHeight = RowHeight(bodyRow, columnWidth);

            if (fit > 0 && used + rowHeight > maxHeight)
            {
                break;
            }

            used += rowHeight;
            fit++;

            if (used > maxHeight)
            {
                break;
            }
        }

        if (fit < 1 || fit >= bodyRows.Count)
        {
            return null;
        }

        List<TableRow> headRows = [.. headerRows, .. bodyRows.GetRange(0, fit)];
        List<TableRow> tailRows = [.. headerRows, .. bodyRows.GetRange(fit, bodyRows.Count - fit)];

        var head = TableBlock.Create(table.ColumnCount, headRows).Value;
        var tail = TableBlock.Create(table.ColumnCount, tailRows).Value;
        return (head, tail);
    }

    private double MeasureBlock(IBlock block, double width) => block switch
    {
        Spacer spacer => spacer.Height,
        Paragraph paragraph => ParagraphHeight(paragraph, width),
        PageNumberField => _measurer.LineHeight(TextStyle.Default),
        ImageBlock image => image.Height,
        ChartBlock chart => chart.Height,
        ListBlock list => ListHeight(list, width),
        TableBlock table => TableHeight(table, width),
        RowBlock row => RowGridHeight(row, width),
        _ => 0d,
    };

    private double ParagraphHeight(Paragraph paragraph, double width)
    {
        var lineHeight = 0d;
        foreach (var run in paragraph.Runs)
        {
            lineHeight = Math.Max(lineHeight, _measurer.LineHeight(run.Style));
        }

        return LineCount(paragraph, width) * lineHeight;
    }

    private int LineCount(Paragraph paragraph, double width)
    {
        var lines = 1;
        var lineWidth = 0d;
        var placedAny = false;

        foreach (var run in paragraph.Runs)
        {
            var style = run.Style;
            var space = _measurer.SpaceWidth(style);
            var span = run.Text.AsSpan();
            var pos = 0;

            while (pos < span.Length)
            {
                while (pos < span.Length && span[pos] == ' ')
                {
                    pos++;
                }

                if (pos >= span.Length)
                {
                    break;
                }

                var wordStart = pos;
                while (pos < span.Length && span[pos] != ' ')
                {
                    pos++;
                }

                var wordWidth = _measurer.MeasureWord(span.Slice(wordStart, pos - wordStart), style);

                if (!placedAny)
                {
                    lineWidth = wordWidth;
                    placedAny = true;
                }
                else if (lineWidth + space + wordWidth <= width)
                {
                    lineWidth += space + wordWidth;
                }
                else
                {
                    lines++;
                    lineWidth = wordWidth;
                }
            }
        }

        return lines;
    }

    private double ListHeight(ListBlock list, double width)
    {
        var height = 0d;
        foreach (var item in list.Items)
        {
            height += BlocksHeight(item.Blocks, width);
        }

        return height;
    }

    private double TableHeight(TableBlock table, double width)
    {
        var columnWidth = width / table.ColumnCount;
        var height = 0d;
        foreach (var row in table.Rows)
        {
            height += RowHeight(row, columnWidth);
        }

        return height;
    }

    private double RowHeight(TableRow row, double columnWidth)
    {
        var height = 0d;
        foreach (var cell in row.Cells)
        {
            height = Math.Max(height, BlocksHeight(cell.Blocks, columnWidth));
        }

        return height;
    }

    private double RowGridHeight(RowBlock row, double width)
    {
        var widths = row.ResolveWidths();
        var height = 0d;
        for (var i = 0; i < row.Columns.Count; i++)
        {
            var columnWidth = widths[i] / Column.MaxWidth * width;
            height = Math.Max(height, BlocksHeight(row.Columns[i].Blocks, columnWidth));
        }

        return height;
    }

    private double BlocksHeight(IReadOnlyList<IBlock> blocks, double width)
    {
        var height = 0d;
        foreach (var block in blocks)
        {
            height += MeasureBlock(block, width);
        }

        return height;
    }

    private Geometry ComputeGeometry(Section section)
    {
        var pageSize = section.PageSize;
        var margins = section.Margins;
        var width = pageSize.Width - margins.Left - margins.Right;
        var height = pageSize.Height - margins.Top - margins.Bottom;

        var furniture = FurnitureHeight(section.Header, width) + FurnitureHeight(section.Footer, width);
        var content = height - furniture;

        return new Geometry(width, content > 0d ? content : height);
    }

    private double FurnitureHeight(PageFurniture? furniture, double width) =>
        furniture is null ? 0d : BlocksHeight(furniture.Blocks, width);

    private IReadOnlyList<IBlock> ResolveFields(IReadOnlyList<IBlock> blocks, int page, int total)
    {
        var hasField = false;
        for (var i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] is PageNumberField)
            {
                hasField = true;
                break;
            }
        }

        if (!hasField)
        {
            return blocks;
        }

        var resolved = new List<IBlock>(blocks.Count);
        foreach (var block in blocks)
        {
            resolved.Add(block is PageNumberField field
                ? Paragraph.FromText(field.Resolve(page, total), alignment: field.Alignment).Value
                : block);
        }

        return resolved;
    }

    private static bool RequiresTotalPages(PdfDocument document)
    {
        foreach (var section in document.Sections)
        {
            if (FurnitureUsesTotalPages(section.Header) || FurnitureUsesTotalPages(section.Footer))
            {
                return true;
            }

            foreach (var block in section.Blocks)
            {
                if (block is PageNumberField { UsesTotalPages: true })
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool FurnitureUsesTotalPages(PageFurniture? furniture)
    {
        if (furniture is null)
        {
            return false;
        }

        foreach (var block in furniture.Blocks)
        {
            if (block is PageNumberField { UsesTotalPages: true })
            {
                return true;
            }
        }

        return false;
    }

    private readonly struct Geometry(double contentWidth, double contentHeight)
    {
        public double ContentWidth { get; } = contentWidth;

        public double ContentHeight { get; } = contentHeight;
    }
}
