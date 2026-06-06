using FluentPdf.Domain.Styling;
using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>
/// A block of text composed of one or more <see cref="TextRun"/>s sharing a horizontal
/// alignment.
/// </summary>
public sealed class Paragraph : IBlock
{
    private Paragraph(IReadOnlyList<TextRun> runs, HorizontalAlignment alignment)
    {
        Runs = runs;
        Alignment = alignment;
    }

    public IReadOnlyList<TextRun> Runs { get; }

    public HorizontalAlignment Alignment { get; }

    /// <summary>Creates a paragraph from explicit runs.</summary>
    public static Result<Paragraph> Create(
        IReadOnlyList<TextRun> runs,
        HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        if (runs is null || runs.Count == 0)
        {
            return DomainErrors.Paragraph.NoRuns;
        }

        if (runs.Any(static run => run is null))
        {
            return Error.NullValue;
        }

        return new Paragraph([.. runs], alignment);
    }

    /// <summary>Creates a single-run paragraph from a string and optional style.</summary>
    public static Result<Paragraph> FromText(
        string text,
        TextStyle? style = null,
        HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        var run = TextRun.Create(text, style);

        if (run.IsFailure)
        {
            return run.Error;
        }

        return new Paragraph([run.Value], alignment);
    }
}
