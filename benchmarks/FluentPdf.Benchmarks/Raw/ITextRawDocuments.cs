using System.Globalization;
using FluentPdf.Adapters.Shared;
using FluentPdf.Samples.Contract;
using FluentPdf.Samples.Invoice;
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

namespace FluentPdf.Benchmarks.Raw;

/// <summary>
/// Hand-written iText 7 documents — the code a caller would write if they used iText directly,
/// without FluentPdf. They reproduce the same documents as the matching samples (same fonts,
/// page sizes, table theme, two-pass running furniture and text), so the benchmark compares
/// like with like and the delta against the FluentPdf pipeline isolates the agnostic layer.
/// </summary>
internal sealed class ITextRawDocuments
{
    private static readonly Color Black = new DeviceRgb(0, 0, 0);
    private static readonly Color MutedColor = new DeviceRgb(0x66, 0x66, 0x66);
    private static readonly Color BorderColor =
        new DeviceRgb(RenderingTheme.BorderColor.R, RenderingTheme.BorderColor.G, RenderingTheme.BorderColor.B);
    private static readonly Color HeaderBackground =
        new DeviceRgb(RenderingTheme.HeaderBackground.R, RenderingTheme.HeaderBackground.G, RenderingTheme.HeaderBackground.B);

    private readonly PdfFont _regular = CreateFont(EmbeddedFonts.Regular);
    private readonly PdfFont _bold = CreateFont(EmbeddedFonts.Bold);

    public byte[] Invoice(InvoiceDto invoice)
    {
        using var stream = new MemoryStream();
        var pdf = new ITextPdfDocument(new PdfWriter(stream));

        var info = pdf.GetDocumentInfo();
        info.SetTitle($"Invoice {invoice.Number}");
        info.SetAuthor("ACME Corp");
        info.SetKeywords($"invoice, {invoice.Number}");

        var size = new PageSize(595.28f, 841.89f);
        var layout = new Document(pdf, size, immediateFlush: false);
        layout.SetMargins(48f, 48f, 48f, 48f);

        var header = new Elem.Table(UnitValue.CreatePercentArray([8f, 4f])).UseAllAvailableWidth();
        header.AddCell(BorderlessCell(MixedParagraph(string.Empty, $"Invoice {invoice.Number}", 11f, bold: true)));
        header.AddCell(BorderlessCell(Para(invoice.CustomerName, _regular, 11f, Black, TextAlignment.RIGHT)));
        layout.Add(header);

        layout.Add(Spacer(12f));

        var table = new Elem.Table(UnitValue.CreatePercentArray(3)).UseAllAvailableWidth();
        table.AddHeaderCell(HeaderCell("Description", TextAlignment.LEFT));
        table.AddHeaderCell(HeaderCell("Qty", TextAlignment.RIGHT));
        table.AddHeaderCell(HeaderCell("Total", TextAlignment.RIGHT));
        foreach (var line in invoice.Lines)
        {
            table.AddCell(BodyCell(line.Description, TextAlignment.LEFT));
            table.AddCell(BodyCell(Number(line.Quantity), TextAlignment.RIGHT));
            table.AddCell(BodyCell(Money(line.Total), TextAlignment.RIGHT));
        }

        layout.Add(table);
        layout.Add(Spacer(8f));
        layout.Add(MixedParagraph(string.Empty, $"Total due: {Money(invoice.Total)}", 11f, bold: true)
            .SetTextAlignment(TextAlignment.RIGHT));

        var total = pdf.GetNumberOfPages();
        for (var page = 1; page <= total; page++)
        {
            DrawFurniture(pdf, page, 48f, 48f, "ACME Corp — 123 Example Street", TextAlignment.LEFT,
                "Thank you for your business.", TextAlignment.LEFT);
        }

        layout.Close();
        return stream.ToArray();
    }

    public byte[] Contract(ContractDto contract)
    {
        using var stream = new MemoryStream();
        var pdf = new ITextPdfDocument(new PdfWriter(stream));

        var info = pdf.GetDocumentInfo();
        info.SetTitle(contract.Title);
        info.SetSubject(contract.Reference);
        info.SetAuthor(contract.Lender.Name);
        info.SetCreator("FluentPdf");
        info.SetKeywords($"facility agreement, {contract.Reference}");

        var size = new PageSize(595.28f, 841.89f);
        var layout = new Document(pdf, size, immediateFlush: false);

        // Cover section (margins 72), then the body section (margins 64) after a page break.
        layout.SetMargins(72f, 72f, 72f, 72f);
        ComposeCover(layout, contract);
        var coverEnd = pdf.GetNumberOfPages();

        layout.Add(new Elem.AreaBreak(size));
        layout.SetMargins(64f, 64f, 64f, 64f);
        for (var i = 0; i < contract.Articles.Count; i++)
        {
            ComposeArticle(layout, i + 1, contract.Articles[i]);
        }

        ComposeSignatures(layout, contract.Signatories);

        // Second pass: number every body page once the final count is known.
        var total = pdf.GetNumberOfPages();
        var headerText = $"{contract.Reference} — {contract.Title}";
        for (var page = coverEnd + 1; page <= total; page++)
        {
            DrawFurniture(pdf, page, 64f, 64f, headerText, TextAlignment.LEFT,
                $"Page {page - coverEnd} of {total - coverEnd}", TextAlignment.CENTER);
        }

        layout.Close();
        return stream.ToArray();
    }

    private void ComposeCover(Document layout, ContractDto contract)
    {
        layout.Add(Spacer(120f));
        layout.Add(Para(contract.Title, _bold, 22f, Black, TextAlignment.LEFT));
        layout.Add(Spacer(8f));
        layout.Add(Para(contract.Reference, _regular, 9f, MutedColor, TextAlignment.LEFT));
        layout.Add(Para($"Dated {contract.Date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)}",
            _regular, 9f, MutedColor, TextAlignment.LEFT));
        layout.Add(Spacer(28f));
        layout.Add(Para("Between", _bold, 11f, Black, TextAlignment.LEFT));
        layout.Add(Para($"{contract.Lender.Name} (the \"{contract.Lender.Role}\")", _regular, 11f, Black, TextAlignment.LEFT));
        layout.Add(Para(contract.Lender.Address, _regular, 11f, Black, TextAlignment.LEFT));
        layout.Add(Spacer(8f));
        layout.Add(Para("and", _bold, 11f, Black, TextAlignment.LEFT));
        layout.Add(Para($"{contract.Borrower.Name} (the \"{contract.Borrower.Role}\")", _regular, 11f, Black, TextAlignment.LEFT));
        layout.Add(Para(contract.Borrower.Address, _regular, 11f, Black, TextAlignment.LEFT));
        layout.Add(Spacer(28f));
        layout.Add(Para("Recitals", _bold, 11f, Black, TextAlignment.LEFT));
        layout.Add(Spacer(4f));

        var index = 0;
        foreach (var recital in contract.Recitals)
        {
            index++;
            var letter = (char)('A' + index - 1);
            layout.Add(MixedParagraph($"({letter})  ", recital, 11f, bold: false));
            layout.Add(Spacer(6f));
        }
    }

    private void ComposeArticle(Document layout, int number, ContractArticle article)
    {
        layout.Add(Para($"Article {number}. {article.Title}", _bold, 13f, Black, TextAlignment.LEFT));
        layout.Add(Spacer(6f));

        for (var i = 0; i < article.Clauses.Count; i++)
        {
            layout.Add(MixedParagraph($"{number}.{i + 1}  ", article.Clauses[i], 11f, bold: false));
            layout.Add(Spacer(6f));
        }

        layout.Add(Spacer(10f));
    }

    private void ComposeSignatures(Document layout, IReadOnlyList<Party> signatories)
    {
        layout.Add(Spacer(16f));
        layout.Add(Para("Execution", _bold, 13f, Black, TextAlignment.LEFT));
        layout.Add(Spacer(6f));
        layout.Add(Para("IN WITNESS WHEREOF the parties have executed this Agreement as a deed "
            + "and have caused it to be delivered on the date stated at the beginning of "
            + "this Agreement.", _regular, 11f, Black, TextAlignment.LEFT));
        layout.Add(Spacer(16f));

        foreach (var party in signatories)
        {
            layout.Add(Para(party.Role, _bold, 11f, Black, TextAlignment.LEFT));
            layout.Add(Para(party.Name, _regular, 11f, Black, TextAlignment.LEFT));
            layout.Add(Spacer(6f));
            layout.Add(Para("Signature: ______________________________", _regular, 11f, Black, TextAlignment.LEFT));
            layout.Add(Para("Name:", _regular, 11f, Black, TextAlignment.LEFT));
            layout.Add(Para("Title:", _regular, 11f, Black, TextAlignment.LEFT));
            layout.Add(Spacer(18f));
        }
    }

    private void DrawFurniture(
        ITextPdfDocument pdf,
        int pageNumber,
        double marginTop,
        double marginBottom,
        string headerText,
        TextAlignment headerAlignment,
        string footerText,
        TextAlignment footerAlignment)
    {
        var page = pdf.GetPage(pageNumber);
        var size = page.GetPageSize();
        using var canvas = new Canvas(new PdfCanvas(page), size);
        canvas.SetFont(_regular).SetFontSize(9f);

        Place(canvas, headerText, headerAlignment, size, (float)(size.GetTop() - marginTop + 6d));
        Place(canvas, footerText, footerAlignment, size, (float)(size.GetBottom() + marginBottom - 14d));
    }

    private static void Place(Canvas canvas, string text, TextAlignment alignment, Rectangle size, float y)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var x = alignment switch
        {
            TextAlignment.CENTER => (size.GetLeft() + size.GetRight()) / 2f,
            TextAlignment.RIGHT => size.GetRight() - 40f,
            _ => size.GetLeft() + 40f,
        };

        canvas.ShowTextAligned(text, x, y, alignment);
    }

    private static Elem.Paragraph Para(string text, PdfFont font, float size, Color color, TextAlignment alignment)
    {
        var run = new Elem.Text(text).SetFont(font).SetFontSize(size).SetFontColor(color);
        return new Elem.Paragraph().SetMargin(0f).SetMultipliedLeading(1.2f).SetTextAlignment(alignment).Add(run);
    }

    private Elem.Paragraph MixedParagraph(string boldPrefix, string body, float size, bool bold)
    {
        var paragraph = new Elem.Paragraph().SetMargin(0f).SetMultipliedLeading(1.2f).SetTextAlignment(TextAlignment.LEFT);

        if (boldPrefix.Length > 0)
        {
            paragraph.Add(new Elem.Text(boldPrefix).SetFont(_bold).SetFontSize(size).SetFontColor(Black));
        }

        paragraph.Add(new Elem.Text(body).SetFont(bold ? _bold : _regular).SetFontSize(size).SetFontColor(Black));
        return paragraph;
    }

    private Elem.Cell HeaderCell(string text, TextAlignment alignment) =>
        StyledCell(text, alignment, header: true);

    private Elem.Cell BodyCell(string text, TextAlignment alignment) =>
        StyledCell(text, alignment, header: false);

    private Elem.Cell StyledCell(string text, TextAlignment alignment, bool header)
    {
        var cell = new Elem.Cell()
            .SetPadding(RenderingTheme.CellPadding)
            .SetBorder(new SolidBorder(BorderColor, RenderingTheme.BorderWidth));
        cell.SetTextAlignment(alignment);

        if (header)
        {
            cell.SetBackgroundColor(HeaderBackground);
        }

        cell.Add(Para(text, _regular, 11f, Black, alignment));
        return cell;
    }

    private static Elem.Cell BorderlessCell(Elem.IBlockElement content) =>
        new Elem.Cell().SetBorder(Border.NO_BORDER).Add(content);

    private static Elem.IBlockElement Spacer(float height) => new Elem.Div().SetHeight(height);

    private static PdfFont CreateFont(byte[] bytes) =>
        PdfFontFactory.CreateFont(bytes, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);

    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
