using FluentPdf.Domain;
using FluentPdf.Domain.Content;

namespace FluentPdf.Application.Rendering;

/// <summary>
/// Computes the set of <see cref="PdfFeature"/>s a document actually uses by walking its
/// sections, headers, footers and nested content. The result is matched against an
/// adapter's <see cref="RendererCapabilities"/> to detect unsupported content up front.
/// </summary>
public static class DocumentFeatureScanner
{
    /// <summary>Returns the union of every feature used anywhere in the document.</summary>
    public static PdfFeature Scan(PdfDocument document)
    {
        if (document is null)
        {
            return PdfFeature.None;
        }

        var features = PdfFeature.None;

        if (HasMetadata(document.Metadata))
        {
            features |= PdfFeature.Metadata;
        }

        foreach (var section in document.Sections)
        {
            if (section.Header is not null)
            {
                features |= PdfFeature.Header | ScanBlocks(section.Header.Blocks);
            }

            if (section.Footer is not null)
            {
                features |= PdfFeature.Footer | ScanBlocks(section.Footer.Blocks);
            }

            features |= ScanBlocks(section.Blocks);
        }

        return features;
    }

    private static bool HasMetadata(DocumentMetadata metadata) =>
        !string.IsNullOrWhiteSpace(metadata.Title)
        || !string.IsNullOrWhiteSpace(metadata.Author)
        || !string.IsNullOrWhiteSpace(metadata.Subject)
        || !string.IsNullOrWhiteSpace(metadata.Creator)
        || metadata.Keywords.Count > 0;

    private static PdfFeature ScanBlocks(IReadOnlyList<IBlock> blocks)
    {
        var features = PdfFeature.None;

        foreach (var block in blocks)
        {
            features |= ScanBlock(block);
        }

        return features;
    }

    private static PdfFeature ScanBlock(IBlock block) => block switch
    {
        Paragraph => PdfFeature.Paragraph,
        Spacer => PdfFeature.Spacer,
        PageBreak => PdfFeature.PageBreak,
        ImageBlock => PdfFeature.Image,
        ChartBlock => PdfFeature.Chart,
        ListBlock list => PdfFeature.List | ScanListItems(list),
        TableBlock table => PdfFeature.Table | ScanTable(table),
        RowBlock row => PdfFeature.Grid | ScanRow(row),
        _ => PdfFeature.None,
    };

    private static PdfFeature ScanRow(RowBlock row)
    {
        var features = PdfFeature.None;

        foreach (var column in row.Columns)
        {
            features |= ScanBlocks(column.Blocks);
        }

        return features;
    }

    private static PdfFeature ScanListItems(ListBlock list)
    {
        var features = PdfFeature.None;

        foreach (var item in list.Items)
        {
            features |= ScanBlocks(item.Blocks);
        }

        return features;
    }

    private static PdfFeature ScanTable(TableBlock table)
    {
        var features = PdfFeature.None;

        foreach (var row in table.Rows)
        {
            foreach (var cell in row.Cells)
            {
                features |= ScanBlocks(cell.Blocks);
            }
        }

        return features;
    }
}
