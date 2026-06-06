using FluentPdf.Domain.Styling;
using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>A single cell of a <see cref="TableBlock"/>, holding block content.</summary>
public sealed class TableCell
{
    private TableCell(IReadOnlyList<IBlock> blocks, HorizontalAlignment alignment)
    {
        Blocks = blocks;
        Alignment = alignment;
    }

    public IReadOnlyList<IBlock> Blocks { get; }

    public HorizontalAlignment Alignment { get; }

    /// <summary>Creates a cell from its block content.</summary>
    public static Result<TableCell> Create(
        IReadOnlyList<IBlock> blocks,
        HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        if (blocks is null || blocks.Count == 0)
        {
            return DomainErrors.Table.EmptyCell;
        }

        if (blocks.Any(static block => block is null))
        {
            return Error.NullValue;
        }

        return new TableCell([.. blocks], alignment);
    }

    /// <summary>Creates a cell containing a single paragraph of text.</summary>
    public static Result<TableCell> FromText(
        string text,
        HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        var paragraph = Paragraph.FromText(text);

        if (paragraph.IsFailure)
        {
            return paragraph.Error;
        }

        return new TableCell([paragraph.Value], alignment);
    }
}
