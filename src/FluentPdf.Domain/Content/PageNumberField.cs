using System.Globalization;
using FluentPdf.Domain.Styling;
using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>
/// A dynamic page-number field, typically placed in a running header or footer (e.g.
/// "Page {page} of {pages}"). It is resolved into concrete text during pagination — once the
/// page index and, if referenced, the total page count are known — so renderers only ever see
/// plain paragraphs and never need a dedicated capability.
/// </summary>
public sealed class PageNumberField : IBlock
{
    /// <summary>The placeholder replaced by the current 1-based page number.</summary>
    public const string PageToken = "{page}";

    /// <summary>The placeholder replaced by the total page count.</summary>
    public const string PagesToken = "{pages}";

    private PageNumberField(string format, HorizontalAlignment alignment)
    {
        Format = format;
        Alignment = alignment;
    }

    /// <summary>The format string, carrying <see cref="PageToken"/> and/or <see cref="PagesToken"/>.</summary>
    public string Format { get; }

    public HorizontalAlignment Alignment { get; }

    /// <summary>Whether the format references the total page count (needs a counting pass).</summary>
    public bool UsesTotalPages => Format.IndexOf(PagesToken, StringComparison.Ordinal) >= 0;

    /// <summary>Creates a page-number field from a non-empty format string.</summary>
    public static Result<PageNumberField> Create(
        string format,
        HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        if (string.IsNullOrEmpty(format))
        {
            return DomainErrors.PageNumber.EmptyFormat;
        }

        return new PageNumberField(format, alignment);
    }

    /// <summary>Resolves the format into text for a given page and total page count.</summary>
    public string Resolve(int page, int totalPages) =>
        Format
            .Replace(PageToken, page.ToString(CultureInfo.InvariantCulture))
            .Replace(PagesToken, totalPages.ToString(CultureInfo.InvariantCulture));
}
