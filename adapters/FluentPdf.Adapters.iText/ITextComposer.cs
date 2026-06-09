using FluentPdf.Adapters.Shared;
using FluentPdf.Application.Rendering;
using FluentPdf.Domain;
using FluentPdf.Domain.Content;
using iText.IO.Font;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Properties;
using Elem = iText.Layout.Element;
using ITextPdfDocument = iText.Kernel.Pdf.PdfDocument;
using DomainColor = FluentPdf.Domain.Styling.Color;
using DomainAlignment = FluentPdf.Domain.Styling.HorizontalAlignment;

namespace FluentPdf.Adapters.IText;

/// <summary>Maps the agnostic document model onto iText layout elements.</summary>
internal sealed class ITextComposer(IChartRenderer? charts)
{
    private readonly PdfFont _regular = CreateFont(EmbeddedFonts.Regular);
    private readonly PdfFont _bold = CreateFont(EmbeddedFonts.Bold);

    public static PageSize PageSizeOf(Section section) =>
        new((float)section.PageSize.Width, (float)section.PageSize.Height);

    private static PdfFont CreateFont(byte[] bytes) =>
        PdfFontFactory.CreateFont(bytes, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);

    private PdfFont FontFor(bool bold) => bold ? _bold : _regular;

    public void ComposeBody(Document layout, IReadOnlyList<IBlock> blocks)
    {
        foreach (var block in blocks)
        {
            if (block is PageBreak)
            {
                layout.Add(new Elem.AreaBreak(AreaBreakType.NEXT_PAGE));
                continue;
            }

            layout.Add(Convert(block));
        }
    }

    public void DrawFurniture(ITextPdfDocument pdf, int pageNumber, Section section, int total)
    {
        if (section.Header is null && section.Footer is null)
        {
            return;
        }

        var page = pdf.GetPage(pageNumber);
        var size = page.GetPageSize();
        using var canvas = new Canvas(new PdfCanvas(page), size);
        canvas.SetFont(_regular).SetFontSize(9f);

        if (section.Header is not null)
        {
            var (text, alignment) = Furniture(section.Header.Blocks, pageNumber, total);
            Place(canvas, text, alignment, size, (float)(size.GetTop() - section.Margins.Top + 6d));
        }

        if (section.Footer is not null)
        {
            var (text, alignment) = Furniture(section.Footer.Blocks, pageNumber, total);
            Place(canvas, text, alignment, size, (float)(size.GetBottom() + section.Margins.Bottom - 14d));
        }
    }

    private void Place(Canvas canvas, string text, DomainAlignment alignment, Rectangle size, float y)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var (x, textAlign) = alignment switch
        {
            DomainAlignment.Center => ((size.GetLeft() + size.GetRight()) / 2f, TextAlignment.CENTER),
            DomainAlignment.Right => (size.GetRight() - 40f, TextAlignment.RIGHT),
            _ => (size.GetLeft() + 40f, TextAlignment.LEFT),
        };

        canvas.ShowTextAligned(text, x, y, textAlign);
    }

    private static (string Text, DomainAlignment Alignment) Furniture(
        IReadOnlyList<IBlock> blocks,
        int pageNumber,
        int total)
    {
        var parts = new List<string>(blocks.Count);
        var alignment = DomainAlignment.Left;

        foreach (var block in blocks)
        {
            switch (block)
            {
                case PageNumberField field:
                    parts.Add(field.Resolve(pageNumber, total));
                    alignment = field.Alignment;
                    break;
                case Paragraph paragraph:
                    parts.Add(string.Concat(paragraph.Runs.Select(static run => run.Text)));
                    alignment = paragraph.Alignment;
                    break;
                default:
                    break;
            }
        }

        return (string.Join("  ", parts), alignment);
    }

    private Elem.IBlockElement Convert(IBlock block) => block switch
    {
        Paragraph paragraph => BuildParagraph(paragraph),
        Spacer spacer => new Elem.Div().SetHeight((float)spacer.Height),
        ImageBlock image => new Elem.Div().Add(BuildImage(image)),
        ListBlock list => BuildList(list),
        TableBlock table => BuildTable(table),
        RowBlock row => BuildGrid(row),
        PageNumberField field => new Elem.Paragraph(field.Format),
        ChartBlock chart => ChartImage(chart),
        _ => new Elem.Paragraph(string.Empty),
    };

    private Elem.IBlockElement ChartImage(ChartBlock chart)
    {
        if (charts is null)
        {
            return new Elem.Paragraph(chart.Title ?? "[chart]");
        }

        var image = new Elem.Image(iText.IO.Image.ImageDataFactory.Create(charts.RenderPng(chart)))
            .SetWidth((float)chart.Width)
            .SetHeight((float)chart.Height);

        return new Elem.Div().Add(image);
    }

    private Elem.Paragraph BuildParagraph(Paragraph paragraph, DomainAlignment? alignmentOverride = null)
    {
        var element = new Elem.Paragraph().SetMargin(0f).SetMultipliedLeading(1.2f);
        element.SetTextAlignment(Map(alignmentOverride ?? paragraph.Alignment));

        foreach (var run in paragraph.Runs)
        {
            element.Add(BuildText(run));
        }

        return element;
    }

    private void AddCellBlocks(Elem.Cell cell, IReadOnlyList<IBlock> blocks, DomainAlignment alignment)
    {
        foreach (var block in blocks)
        {
            cell.Add(block is Paragraph paragraph ? BuildParagraph(paragraph, alignment) : Convert(block));
        }
    }

    private Elem.Text BuildText(TextRun run)
    {
        var style = run.Style;
        var text = new Elem.Text(run.Text)
            .SetFont(FontFor(style.IsBold))
            .SetFontSize((float)style.FontSize)
            .SetFontColor(Rgb(style.Color));

        if (style.IsItalic)
        {
            text.SetItalic(); // faux italic (skew) — italic weights are not embedded
        }

        if (style.IsUnderlined)
        {
            text.SetUnderline();
        }

        return text;
    }

    private Elem.List BuildList(ListBlock list)
    {
        var element = list.Style == ListStyle.Ordered
            ? new Elem.List(ListNumberingType.DECIMAL).SetItemStartIndex(list.StartNumber)
            : new Elem.List();

        foreach (var item in list.Items)
        {
            var listItem = new Elem.ListItem();
            AddBlocks(listItem.Add, item.Blocks);
            element.Add(listItem);
        }

        return element;
    }

    private Elem.Table BuildTable(TableBlock table)
    {
        var element = new Elem.Table(UnitValue.CreatePercentArray(table.ColumnCount))
            .UseAllAvailableWidth();

        foreach (var row in table.Rows)
        {
            foreach (var tableCell in row.Cells)
            {
                var cell = new Elem.Cell()
                    .SetPadding(RenderingTheme.CellPadding)
                    .SetBorder(new SolidBorder(Rgb(RenderingTheme.BorderColor), RenderingTheme.BorderWidth));
                cell.SetTextAlignment(Map(tableCell.Alignment));

                if (row.IsHeader)
                {
                    cell.SetBackgroundColor(Rgb(RenderingTheme.HeaderBackground));
                }

                // The cell's alignment governs its content (matching the QuestPDF adapter), so
                // paragraphs adopt it rather than their own default.
                AddCellBlocks(cell, tableCell.Blocks, tableCell.Alignment);

                if (row.IsHeader)
                {
                    element.AddHeaderCell(cell);
                }
                else
                {
                    element.AddCell(cell);
                }
            }
        }

        return element;
    }

    private Elem.Table BuildGrid(RowBlock row)
    {
        var widths = new float[row.Columns.Count];
        var resolved = row.ResolveWidths();
        for (var i = 0; i < widths.Length; i++)
        {
            widths[i] = (float)resolved[i];
        }

        var element = new Elem.Table(UnitValue.CreatePercentArray(widths)).UseAllAvailableWidth();

        foreach (var column in row.Columns)
        {
            var cell = new Elem.Cell().SetBorder(Border.NO_BORDER);
            AddBlocks(cell.Add, column.Blocks);
            element.AddCell(cell);
        }

        return element;
    }

    private Elem.Image BuildImage(ImageBlock image) =>
        new Elem.Image(iText.IO.Image.ImageDataFactory.Create([.. image.Data]))
            .SetWidth((float)image.Width)
            .SetHeight((float)image.Height);

    private void AddBlocks(Func<Elem.IBlockElement, object> addBlock, IReadOnlyList<IBlock> blocks)
    {
        foreach (var block in blocks)
        {
            if (block is PageBreak)
            {
                continue;
            }

            addBlock(Convert(block));
        }
    }

    private static Color Rgb(DomainColor color) => new DeviceRgb(color.Red, color.Green, color.Blue);

    private static Color Rgb((byte R, byte G, byte B) color) => new DeviceRgb(color.R, color.G, color.B);

    private static TextAlignment Map(DomainAlignment alignment) => alignment switch
    {
        DomainAlignment.Center => TextAlignment.CENTER,
        DomainAlignment.Right => TextAlignment.RIGHT,
        _ => TextAlignment.LEFT,
    };
}
