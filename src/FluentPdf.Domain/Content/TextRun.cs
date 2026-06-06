using FluentPdf.Domain.Styling;
using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>
/// A contiguous run of text sharing a single <see cref="TextStyle"/>. Paragraphs are
/// composed of one or more runs, which lets a single line mix styles (e.g. a bold word).
/// </summary>
public sealed class TextRun : ValueObject
{
    private TextRun(string text, TextStyle style)
    {
        Text = text;
        Style = style;
    }

    public string Text { get; }

    public TextStyle Style { get; }

    /// <summary>Creates a text run, defaulting to <see cref="TextStyle.Default"/>.</summary>
    public static Result<TextRun> Create(string text, TextStyle? style = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return DomainErrors.TextRun.EmptyText;
        }

        return new TextRun(text, style ?? TextStyle.Default);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Text;
        yield return Style;
    }
}
