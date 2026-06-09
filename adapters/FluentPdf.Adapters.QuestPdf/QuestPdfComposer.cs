using System.Globalization;
using FluentPdf.Adapters.Shared;
using FluentPdf.Application.Rendering;
using FluentPdf.Domain;
using FluentPdf.Domain.Content;
using FluentPdf.Domain.Styling;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using DomainAlignment = FluentPdf.Domain.Styling.HorizontalAlignment;
using DomainColor = FluentPdf.Domain.Styling.Color;
using DomainDocument = FluentPdf.Domain.PdfDocument;
using DomainMetadata = FluentPdf.Domain.DocumentMetadata;
using DomainTextStyle = FluentPdf.Domain.Styling.TextStyle;
using QuestMetadata = QuestPDF.Infrastructure.DocumentMetadata;

namespace FluentPdf.Adapters.QuestPdf;

/// <summary>Translates the agnostic document model into QuestPDF's fluent layout API.</summary>
internal sealed class QuestPdfComposer(IChartRenderer? charts)
{
    public void Compose(IDocumentContainer container, DomainDocument document)
    {
        foreach (var section in document.Sections)
        {
            container.Page(page =>
            {
                page.Size((float)section.PageSize.Width, (float)section.PageSize.Height, Unit.Point);
                page.MarginTop((float)section.Margins.Top, Unit.Point);
                page.MarginRight((float)section.Margins.Right, Unit.Point);
                page.MarginBottom((float)section.Margins.Bottom, Unit.Point);
                page.MarginLeft((float)section.Margins.Left, Unit.Point);
                page.DefaultTextStyle(text => text
                    .FontFamily(EmbeddedFonts.Family)
                    .FontSize((float)DomainTextStyle.DefaultFontSize));

                if (section.Header is not null)
                {
                    ComposeBlocks(page.Header(), section.Header.Blocks);
                }

                ComposeBlocks(page.Content(), section.Blocks);

                if (section.Footer is not null)
                {
                    ComposeBlocks(page.Footer(), section.Footer.Blocks);
                }
            });
        }
    }

    public static QuestMetadata Metadata(DomainMetadata metadata) => new()
    {
        Title = metadata.Title,
        Author = metadata.Author,
        Subject = metadata.Subject,
        Creator = metadata.Creator,
        Keywords = metadata.Keywords.Count > 0 ? string.Join(", ", metadata.Keywords) : null,
    };

    private void ComposeBlocks(IContainer container, IReadOnlyList<IBlock> blocks) =>
        container.Column(column =>
        {
            foreach (var block in blocks)
            {
                ComposeBlock(column.Item(), block);
            }
        });

    private void ComposeBlock(IContainer container, IBlock block)
    {
        switch (block)
        {
            case Paragraph paragraph:
                ComposeParagraph(container, paragraph);
                break;
            case Spacer spacer:
                container.Height((float)spacer.Height);
                break;
            case PageBreak:
                container.PageBreak();
                break;
            case PageNumberField field:
                ComposePageNumber(container, field);
                break;
            case ImageBlock image:
                container.Width((float)image.Width).Image([.. image.Data]);
                break;
            case ListBlock list:
                ComposeList(container, list);
                break;
            case TableBlock table:
                ComposeTable(container, table);
                break;
            case RowBlock row:
                ComposeRow(container, row);
                break;
            case ChartBlock chart when charts is not null:
                // Cap to the chart's intrinsic size, then fit within the available area
                // (preserving aspect) so it never overflows the page in either dimension.
                container
                    .MaxWidth((float)chart.Width)
                    .MaxHeight((float)chart.Height)
                    .Image(charts.RenderPng(chart))
                    .FitArea();
                break;
            case ChartBlock chart:
                // No chart renderer supplied (capability not advertised); placeholder for safety.
                container.Text(chart.Title ?? "[chart]");
                break;
            default:
                break;
        }
    }

    private static void ComposeParagraph(IContainer container, Paragraph paragraph) =>
        container.Text(text =>
        {
            Align(text, paragraph.Alignment);

            foreach (var run in paragraph.Runs)
            {
                Style(text.Span(run.Text), run.Style);
            }
        });

    private static void ComposePageNumber(IContainer container, PageNumberField field) =>
        container.Text(text =>
        {
            Align(text, field.Alignment);

            foreach (var (literal, token) in Tokenize(field.Format))
            {
                if (token == PageNumberField.PageToken)
                {
                    text.CurrentPageNumber();
                }
                else if (token == PageNumberField.PagesToken)
                {
                    text.TotalPages();
                }
                else
                {
                    text.Span(literal);
                }
            }
        });

    private void ComposeList(IContainer container, ListBlock list) =>
        container.Column(column =>
        {
            var number = list.StartNumber;

            foreach (var item in list.Items)
            {
                var marker = list.Style == ListStyle.Ordered
                    ? $"{number.ToString(CultureInfo.InvariantCulture)}."
                    : "•";

                column.Item().Row(row =>
                {
                    row.ConstantItem(18f).Text(marker);
                    ComposeBlocks(row.RelativeItem(), item.Blocks);
                });

                number++;
            }
        });

    private void ComposeTable(IContainer container, TableBlock table) =>
        container.Table(descriptor =>
        {
            descriptor.ColumnsDefinition(columns =>
            {
                for (var i = 0; i < table.ColumnCount; i++)
                {
                    columns.RelativeColumn();
                }
            });

            foreach (var headerRow in table.Rows)
            {
                if (!headerRow.IsHeader)
                {
                    continue;
                }

                descriptor.Header(header =>
                {
                    foreach (var cell in headerRow.Cells)
                    {
                        ComposeCell(header.Cell(), cell, isHeader: true);
                    }
                });
            }

            foreach (var row in table.Rows)
            {
                if (row.IsHeader)
                {
                    continue;
                }

                foreach (var cell in row.Cells)
                {
                    ComposeCell(descriptor.Cell(), cell, isHeader: false);
                }
            }
        });

    private void ComposeCell(IContainer container, TableCell cell, bool isHeader)
    {
        var styled = container
            .Border(RenderingTheme.BorderWidth)
            .BorderColor(Hex(RenderingTheme.BorderColor));

        if (isHeader)
        {
            styled = styled.Background(Hex(RenderingTheme.HeaderBackground));
        }

        ComposeBlocks(Align(styled.Padding(RenderingTheme.CellPadding), cell.Alignment), cell.Blocks);
    }

    private void ComposeRow(IContainer container, RowBlock row) =>
        container.Row(rowDescriptor =>
        {
            var widths = row.ResolveWidths();

            for (var i = 0; i < row.Columns.Count; i++)
            {
                ComposeBlocks(rowDescriptor.RelativeItem((float)widths[i]), row.Columns[i].Blocks);
            }
        });

    private static IContainer Align(IContainer container, DomainAlignment alignment) => alignment switch
    {
        DomainAlignment.Center => container.AlignCenter(),
        DomainAlignment.Right => container.AlignRight(),
        _ => container.AlignLeft(),
    };

    private static void Align(TextDescriptor text, DomainAlignment alignment)
    {
        switch (alignment)
        {
            case DomainAlignment.Center:
                text.AlignCenter();
                break;
            case DomainAlignment.Right:
                text.AlignRight();
                break;
            default:
                text.AlignLeft();
                break;
        }
    }

    private static void Style(TextSpanDescriptor span, DomainTextStyle style)
    {
        span.FontFamily(EmbeddedFonts.Family).FontSize((float)style.FontSize).FontColor(Hex(style.Color));

        if (style.IsBold)
        {
            span.Bold();
        }

        if (style.IsItalic)
        {
            span.Italic();
        }

        if (style.IsUnderlined)
        {
            span.Underline();
        }
    }

    private static string Hex(DomainColor color) =>
        $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";

    private static string Hex((byte R, byte G, byte B) color) =>
        $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static IEnumerable<(string Literal, string? Token)> Tokenize(string format)
    {
        var index = 0;

        while (index < format.Length)
        {
            var page = format.IndexOf(PageNumberField.PageToken, index, StringComparison.Ordinal);
            var pages = format.IndexOf(PageNumberField.PagesToken, index, StringComparison.Ordinal);

            // {pages} contains {page} as a prefix, so prefer the longer match when they coincide.
            var nextToken = ChooseToken(page, pages);

            if (nextToken.Position < 0)
            {
                yield return (format.Substring(index), null);
                yield break;
            }

            if (nextToken.Position > index)
            {
                yield return (format.Substring(index, nextToken.Position - index), null);
            }

            yield return (string.Empty, nextToken.Token);
            index = nextToken.Position + nextToken.Token.Length;
        }
    }

    private static (int Position, string Token) ChooseToken(int pagePosition, int pagesPosition)
    {
        if (pagesPosition >= 0 && (pagePosition < 0 || pagesPosition <= pagePosition))
        {
            return (pagesPosition, PageNumberField.PagesToken);
        }

        return (pagePosition, PageNumberField.PageToken);
    }
}
