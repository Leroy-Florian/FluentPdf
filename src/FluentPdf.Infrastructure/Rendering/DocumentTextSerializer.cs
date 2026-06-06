using System.Globalization;
using System.Text;
using FluentPdf.Application.Layout;
using FluentPdf.Domain;
using FluentPdf.Domain.Content;

namespace FluentPdf.Infrastructure.Rendering;

/// <summary>
/// Produces a deterministic, human-readable text projection of an already-paginated document.
/// The in-memory renderer uses it as its "rendered" payload: every visible string appears
/// verbatim, page by page, which lets the adapter conformance suite assert that text and
/// pagination survive rendering without a real PDF parser.
/// </summary>
internal static class DocumentTextSerializer
{
    /// <summary>
    /// Serializes the document's metadata followed by each laid-out page, returning the page
    /// count. Pages are consumed lazily from <paramref name="pages"/>, so memory stays flat.
    /// </summary>
    public static int Serialize(StringBuilder builder, PdfDocument document, IEnumerable<LaidOutPage> pages)
    {
        AppendMetadata(builder, document.Metadata);

        var count = 0;

        foreach (var page in pages)
        {
            count++;
            builder.AppendLine($"[page {page.Number}]");

            if (page.Header.Count > 0)
            {
                builder.AppendLine("[header]");
                AppendBlocks(builder, page.Header);
            }

            AppendBlocks(builder, page.Body);

            if (page.Footer.Count > 0)
            {
                builder.AppendLine("[footer]");
                AppendBlocks(builder, page.Footer);
            }
        }

        return count;
    }

    private static void AppendMetadata(StringBuilder builder, DocumentMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.Title))
        {
            builder.AppendLine($"title: {metadata.Title}");
        }

        if (!string.IsNullOrWhiteSpace(metadata.Author))
        {
            builder.AppendLine($"author: {metadata.Author}");
        }

        if (!string.IsNullOrWhiteSpace(metadata.Subject))
        {
            builder.AppendLine($"subject: {metadata.Subject}");
        }

        if (!string.IsNullOrWhiteSpace(metadata.Creator))
        {
            builder.AppendLine($"creator: {metadata.Creator}");
        }

        if (metadata.Keywords.Count > 0)
        {
            builder.AppendLine($"keywords: {string.Join(", ", metadata.Keywords)}");
        }
    }

    private static void AppendBlocks(StringBuilder builder, IReadOnlyList<IBlock> blocks)
    {
        foreach (var block in blocks)
        {
            AppendBlock(builder, block);
        }
    }

    private static void AppendBlock(StringBuilder builder, IBlock block)
    {
        switch (block)
        {
            case Paragraph paragraph:
                builder.AppendLine(string.Concat(paragraph.Runs.Select(static run => run.Text)));
                break;
            case Spacer spacer:
                builder.AppendLine($"[spacer:{spacer.Height}]");
                break;
            case PageBreak:
                builder.AppendLine("[page-break]");
                break;
            case PageNumberField field:
                builder.AppendLine(field.Format);
                break;
            case ImageBlock image:
                builder.AppendLine($"[image:{image.Format} {image.Width}x{image.Height}]");
                break;
            case ChartBlock chart:
                AppendChart(builder, chart);
                break;
            case ListBlock list:
                AppendList(builder, list);
                break;
            case TableBlock table:
                AppendTable(builder, table);
                break;
            case RowBlock row:
                AppendRow(builder, row);
                break;
            default:
                builder.AppendLine($"[unknown:{block.GetType().Name}]");
                break;
        }
    }

    private static void AppendList(StringBuilder builder, ListBlock list)
    {
        var index = 1;

        foreach (var item in list.Items)
        {
            var marker = list.Style == ListStyle.Ordered ? $"{index}." : "-";
            builder.AppendLine($"{marker} ");
            AppendBlocks(builder, item.Blocks);
            index++;
        }
    }

    private static void AppendTable(StringBuilder builder, TableBlock table)
    {
        foreach (var row in table.Rows)
        {
            builder.AppendLine(row.IsHeader ? "[table-header]" : "[table-row]");

            foreach (var cell in row.Cells)
            {
                AppendBlocks(builder, cell.Blocks);
            }
        }
    }

    private static void AppendChart(StringBuilder builder, ChartBlock chart)
    {
        var title = string.IsNullOrEmpty(chart.Title) ? string.Empty : $" {chart.Title}";
        builder.AppendLine($"[chart:{chart.Type} {chart.Width}x{chart.Height}]{title}");
        builder.AppendLine($"categories: {string.Join(", ", chart.Categories)}");

        foreach (var series in chart.Series)
        {
            var values = string.Join(
                ", ",
                series.Values.Select(static value => value.ToString(CultureInfo.InvariantCulture)));
            builder.AppendLine($"series {series.Name}: {values}");
        }
    }

    private static void AppendRow(StringBuilder builder, RowBlock row)
    {
        var widths = row.ResolveWidths();

        for (var i = 0; i < row.Columns.Count; i++)
        {
            builder.AppendLine($"[col:{widths[i]}]");
            AppendBlocks(builder, row.Columns[i].Blocks);
        }
    }
}
