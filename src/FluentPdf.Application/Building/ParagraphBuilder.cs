using FluentPdf.Domain.Content;
using FluentPdf.Domain.Styling;
using FluentPdf.Kernel;

namespace FluentPdf.Application.Building;

/// <summary>
/// Fluent builder for a <see cref="Paragraph"/> composed of one or more styled runs, which
/// lets a single line mix styles (e.g. a bold word inside normal text).
/// </summary>
public sealed class ParagraphBuilder
{
    private readonly List<Result<TextRun>> _runs = [];
    private HorizontalAlignment _alignment = HorizontalAlignment.Left;

    /// <summary>Sets the paragraph's horizontal alignment.</summary>
    public ParagraphBuilder Align(HorizontalAlignment alignment)
    {
        _alignment = alignment;
        return this;
    }

    /// <summary>Appends a run of text with an optional explicit style.</summary>
    public ParagraphBuilder Run(string text, TextStyle? style = null)
    {
        _runs.Add(TextRun.Create(text, style));
        return this;
    }

    /// <summary>Appends a run with a style configured fluently from the default style.</summary>
    public ParagraphBuilder Run(string text, Action<TextStyleBuilder> configure)
    {
        if (configure is null)
        {
            _runs.Add(Result.Failure<TextRun>(Error.NullValue));
            return this;
        }

        var builder = new TextStyleBuilder();
        configure(builder);
        return Run(text, builder.Build());
    }

    /// <summary>Appends a run using the default style.</summary>
    public ParagraphBuilder Text(string text) => Run(text);

    /// <summary>Appends a bold run.</summary>
    public ParagraphBuilder Bold(string text) => Run(text, TextStyle.Default.WithBold());

    /// <summary>Appends an italic run.</summary>
    public ParagraphBuilder Italic(string text) => Run(text, TextStyle.Default.WithItalic());

    /// <summary>Appends an underlined run.</summary>
    public ParagraphBuilder Underline(string text) => Run(text, TextStyle.Default.WithUnderline());

    /// <summary>Appends a run in the given colour.</summary>
    public ParagraphBuilder Colored(string text, Color color) =>
        Run(text, TextStyle.Default.WithColor(color));

    internal Result<Paragraph> Build()
    {
        var runs = ResultList.Collect(_runs);

        if (runs.IsFailure)
        {
            return runs.Error;
        }

        return Paragraph.Create(runs.Value, _alignment);
    }
}
