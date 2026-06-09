using System.Globalization;
using FluentPdf.Adapters.Shared;
using FluentPdf.Application.Rendering;
using FluentPdf.Domain.Content;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using DomainAlignment = FluentPdf.Domain.Styling.HorizontalAlignment;
using DomainColor = FluentPdf.Domain.Styling.Color;
using DomainDocument = FluentPdf.Domain.PdfDocument;
using DomainMetadata = FluentPdf.Domain.DocumentMetadata;
using DomainSection = FluentPdf.Domain.Section;
using DomainTextStyle = FluentPdf.Domain.Styling.TextStyle;
using MigraColor = MigraDoc.DocumentObjectModel.Color;
using MigraDocument = MigraDoc.DocumentObjectModel.Document;
using MigraParagraph = MigraDoc.DocumentObjectModel.Paragraph;
using PageBreak = FluentPdf.Domain.Content.PageBreak;
using Paragraph = FluentPdf.Domain.Content.Paragraph;

namespace FluentPdf.Adapters.PdfSharp;

/// <summary>Translates the agnostic document model into a MigraDoc document.</summary>
internal sealed class PdfSharpComposer(IChartRenderer? charts)
{
    // The content width of the section currently being composed; used to size table and
    // grid columns (MigraDoc columns are absolute, not relative).
    private double _usableWidth;

    public MigraDocument Compose(DomainDocument document)
    {
        var doc = new MigraDocument();
        ApplyMetadata(doc, document.Metadata);
        ApplyStyles(doc);

        foreach (var section in document.Sections)
        {
            var migra = doc.AddSection();
            ConfigurePage(migra, section);
            _usableWidth = section.PageSize.Width - section.Margins.Left - section.Margins.Right;

            if (section.Header is not null)
            {
                ComposeBlocks(migra.Headers.Primary.Elements, section.Header.Blocks);
            }

            if (section.Footer is not null)
            {
                ComposeBlocks(migra.Footers.Primary.Elements, section.Footer.Blocks);
            }

            ComposeBlocks(migra.Elements, section.Blocks);
        }

        return doc;
    }

    private static void ApplyMetadata(MigraDocument doc, DomainMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.Title))
        {
            doc.Info.Title = metadata.Title;
        }

        if (!string.IsNullOrWhiteSpace(metadata.Author))
        {
            doc.Info.Author = metadata.Author;
        }

        if (!string.IsNullOrWhiteSpace(metadata.Subject))
        {
            doc.Info.Subject = metadata.Subject;
        }

        if (metadata.Keywords.Count > 0)
        {
            doc.Info.Keywords = string.Join(", ", metadata.Keywords);
        }
    }

    private static void ApplyStyles(MigraDocument doc)
    {
        // Drive every base style through the embedded font so all text shares its metrics.
        foreach (var name in (string[])[StyleNames.Normal, StyleNames.Header, StyleNames.Footer])
        {
            var font = doc.Styles[name]!.Font;
            font.Name = EmbeddedFonts.Family;
            font.Size = Unit.FromPoint(DomainTextStyle.DefaultFontSize);
        }
    }

    private static void ConfigurePage(Section migra, DomainSection section)
    {
        var setup = migra.PageSetup;
        setup.PageWidth = Unit.FromPoint(section.PageSize.Width);
        setup.PageHeight = Unit.FromPoint(section.PageSize.Height);
        setup.TopMargin = Unit.FromPoint(section.Margins.Top);
        setup.BottomMargin = Unit.FromPoint(section.Margins.Bottom);
        setup.LeftMargin = Unit.FromPoint(section.Margins.Left);
        setup.RightMargin = Unit.FromPoint(section.Margins.Right);
    }

    private void ComposeBlocks(DocumentElements elements, IReadOnlyList<IBlock> blocks)
    {
        foreach (var block in blocks)
        {
            ComposeBlock(elements, block);
        }
    }

    private void ComposeBlock(DocumentElements elements, IBlock block)
    {
        switch (block)
        {
            case Paragraph paragraph:
                ComposeParagraph(elements, paragraph);
                break;
            case Spacer spacer:
                ComposeSpacer(elements, spacer);
                break;
            case PageBreak:
                // An empty paragraph forced onto a new page realises the break in MigraDoc's
                // flow model (everything after it follows on the new page).
                elements.AddParagraph().Format.PageBreakBefore = true;
                break;
            case PageNumberField field:
                ComposePageNumber(elements, field);
                break;
            case ImageBlock image:
                ComposeImage(elements, [.. image.Data], image.Width, image.Height, image.Alignment);
                break;
            case ListBlock list:
                ComposeList(elements, list);
                break;
            case TableBlock table:
                ComposeTable(elements, table);
                break;
            case RowBlock row:
                ComposeRow(elements, row);
                break;
            case ChartBlock chart when charts is not null:
                ComposeImage(elements, charts.RenderPng(chart), chart.Width, chart.Height, DomainAlignment.Left);
                break;
            case ChartBlock chart:
                // No chart renderer supplied (capability not advertised); placeholder for safety.
                elements.AddParagraph(chart.Title ?? "[chart]");
                break;
            default:
                break;
        }
    }

    private static void ComposeParagraph(
        DocumentElements elements,
        Paragraph paragraph,
        DomainAlignment? alignmentOverride = null)
    {
        var p = elements.AddParagraph();
        p.Format.Alignment = Map(alignmentOverride ?? paragraph.Alignment);

        foreach (var run in paragraph.Runs)
        {
            AddRun(p, run);
        }
    }

    private static void AddRun(MigraParagraph paragraph, TextRun run)
    {
        var style = run.Style;
        var text = paragraph.AddFormattedText(run.Text);

        text.Font.Name = EmbeddedFonts.Family;
        text.Font.Size = Unit.FromPoint(style.FontSize);
        text.Font.Color = Rgb(style.Color);
        text.Font.Bold = style.IsBold;
        text.Font.Italic = style.IsItalic; // faux italic (skew) — italic weight is not embedded

        if (style.IsUnderlined)
        {
            text.Font.Underline = Underline.Single;
        }
    }

    private static void ComposeSpacer(DocumentElements elements, Spacer spacer)
    {
        var p = elements.AddParagraph();
        p.Format.LineSpacingRule = LineSpacingRule.Exactly;
        p.Format.LineSpacing = Unit.FromPoint(spacer.Height);
    }

    private static void ComposePageNumber(DocumentElements elements, PageNumberField field)
    {
        var p = elements.AddParagraph();
        p.Format.Alignment = Map(field.Alignment);

        foreach (var (literal, token) in Tokenize(field.Format))
        {
            if (token == PageNumberField.PageToken)
            {
                p.AddPageField();
            }
            else if (token == PageNumberField.PagesToken)
            {
                p.AddNumPagesField();
            }
            else if (literal.Length > 0)
            {
                p.AddText(literal);
            }
        }
    }

    private static void ComposeImage(
        DocumentElements elements,
        byte[] data,
        double width,
        double height,
        DomainAlignment alignment)
    {
        var p = elements.AddParagraph();
        p.Format.Alignment = Map(alignment);

        // MigraDoc loads in-memory images via the "base64:" pseudo-path (PNG decoded by PDFsharp).
        var image = p.AddImage("base64:" + Convert.ToBase64String(data));
        image.Width = Unit.FromPoint(width);
        image.Height = Unit.FromPoint(height);
        image.LockAspectRatio = false;
    }

    private void ComposeList(DocumentElements elements, ListBlock list)
    {
        var number = list.StartNumber;

        foreach (var item in list.Items)
        {
            var marker = list.Style == ListStyle.Ordered
                ? number.ToString(CultureInfo.InvariantCulture) + "."
                : "•";

            // A borderless two-column table gives the hanging-indent layout the QuestPDF
            // adapter produces with a fixed marker column and a flowing content column.
            var table = elements.AddTable();
            table.Borders.Visible = false;
            table.AddColumn(Unit.FromPoint(18));
            table.AddColumn(Unit.FromPoint(Math.Max(1d, _usableWidth - 18d)));

            var row = table.AddRow();
            row.Cells[0].AddParagraph(marker);
            ComposeBlocks(row.Cells[1].Elements, item.Blocks);

            number++;
        }
    }

    private void ComposeTable(DocumentElements elements, TableBlock table)
    {
        var migra = elements.AddTable();
        var columnWidth = Unit.FromPoint(_usableWidth / table.ColumnCount);

        for (var i = 0; i < table.ColumnCount; i++)
        {
            migra.AddColumn(columnWidth);
        }

        foreach (var row in table.Rows)
        {
            var migraRow = migra.AddRow();
            migraRow.HeadingFormat = row.IsHeader; // repeats header rows across pages

            for (var c = 0; c < row.Cells.Count; c++)
            {
                var cell = migraRow.Cells[c];
                var domainCell = row.Cells[c];

                cell.Borders.Width = RenderingTheme.BorderWidth;
                cell.Borders.Color = Rgb(RenderingTheme.BorderColor);
                cell.Format.Alignment = Map(domainCell.Alignment);

                if (row.IsHeader)
                {
                    cell.Shading.Color = Rgb(RenderingTheme.HeaderBackground);
                }

                ComposeCellBlocks(cell, domainCell.Blocks, domainCell.Alignment);
            }
        }
    }

    private void ComposeRow(DocumentElements elements, RowBlock row)
    {
        var migra = elements.AddTable();
        migra.Borders.Visible = false;

        var widths = row.ResolveWidths();
        var total = widths.Sum();

        foreach (var width in widths)
        {
            migra.AddColumn(Unit.FromPoint(_usableWidth * (width / total)));
        }

        var migraRow = migra.AddRow();

        for (var i = 0; i < row.Columns.Count; i++)
        {
            ComposeBlocks(migraRow.Cells[i].Elements, row.Columns[i].Blocks);
        }
    }

    private void ComposeCellBlocks(Cell cell, IReadOnlyList<IBlock> blocks, DomainAlignment alignment)
    {
        // The cell's alignment governs its content (matching the other adapters), so paragraphs
        // adopt it rather than their own default.
        foreach (var block in blocks)
        {
            if (block is Paragraph paragraph)
            {
                ComposeParagraph(cell.Elements, paragraph, alignment);
            }
            else
            {
                ComposeBlock(cell.Elements, block);
            }
        }
    }

    private static ParagraphAlignment Map(DomainAlignment alignment) => alignment switch
    {
        DomainAlignment.Center => ParagraphAlignment.Center,
        DomainAlignment.Right => ParagraphAlignment.Right,
        _ => ParagraphAlignment.Left,
    };

    private static MigraColor Rgb(DomainColor color) => new(color.Red, color.Green, color.Blue);

    private static MigraColor Rgb((byte R, byte G, byte B) color) => new(color.R, color.G, color.B);

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
