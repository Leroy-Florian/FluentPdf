using FluentPdf.Kernel;

namespace FluentPdf.Domain;

/// <summary>
/// Central catalogue of the domain's business errors, grouped by concept. Keeping codes
/// in one place makes them discoverable and keeps messages consistent.
/// </summary>
public static class DomainErrors
{
    public static class Color
    {
        public static readonly Error ComponentOutOfRange = Error.Validation(
            "Color.ComponentOutOfRange",
            "Each colour component must be between 0 and 255.");

        public static readonly Error AlphaOutOfRange = Error.Validation(
            "Color.AlphaOutOfRange",
            "The alpha component must be between 0 and 1.");

        public static readonly Error InvalidHex = Error.Validation(
            "Color.InvalidHex",
            "The hex colour must be in the form '#RGB', '#RRGGBB' or '#RRGGBBAA'.");
    }

    public static class TextStyle
    {
        public static readonly Error EmptyFontFamily = Error.Validation(
            "TextStyle.EmptyFontFamily",
            "The font family must not be empty.");

        public static readonly Error NonPositiveFontSize = Error.Validation(
            "TextStyle.NonPositiveFontSize",
            "The font size must be greater than zero.");
    }

    public static class PageSize
    {
        public static readonly Error NonPositiveDimension = Error.Validation(
            "PageSize.NonPositiveDimension",
            "Page width and height must be greater than zero.");
    }

    public static class Margins
    {
        public static readonly Error NegativeValue = Error.Validation(
            "Margins.NegativeValue",
            "Margins must not be negative.");
    }

    public static class TextRun
    {
        public static readonly Error EmptyText = Error.Validation(
            "TextRun.EmptyText",
            "A text run must contain text.");
    }

    public static class Paragraph
    {
        public static readonly Error NoRuns = Error.Validation(
            "Paragraph.NoRuns",
            "A paragraph must contain at least one text run.");
    }

    public static class Image
    {
        public static readonly Error EmptyData = Error.Validation(
            "Image.EmptyData",
            "Image data must not be empty.");

        public static readonly Error NonPositiveDimension = Error.Validation(
            "Image.NonPositiveDimension",
            "Image width and height must be greater than zero.");
    }

    public static class Spacer
    {
        public static readonly Error NonPositiveHeight = Error.Validation(
            "Spacer.NonPositiveHeight",
            "Spacer height must be greater than zero.");
    }

    public static class Table
    {
        public static readonly Error NoColumns = Error.Validation(
            "Table.NoColumns",
            "A table must declare at least one column.");

        public static readonly Error NoRows = Error.Validation(
            "Table.NoRows",
            "A table must contain at least one row.");

        public static readonly Error RowWidthMismatch = Error.Validation(
            "Table.RowWidthMismatch",
            "Every row must have exactly as many cells as the table has columns.");

        public static readonly Error EmptyCell = Error.Validation(
            "Table.EmptyCell",
            "A table cell must contain at least one block of content.");
    }

    public static class ListBlock
    {
        public static readonly Error NoItems = Error.Validation(
            "List.NoItems",
            "A list must contain at least one item.");
    }

    public static class Section
    {
        public static readonly Error NoBlocks = Error.Validation(
            "Section.NoBlocks",
            "A section must contain at least one block of content.");
    }

    public static class Document
    {
        public static readonly Error NoSections = Error.Validation(
            "Document.NoSections",
            "A document must contain at least one section.");
    }

    public static class Chart
    {
        public static readonly Error NoCategories = Error.Validation(
            "Chart.NoCategories",
            "A chart must define at least one category.");

        public static readonly Error EmptyCategory = Error.Validation(
            "Chart.EmptyCategory",
            "Chart category labels must not be empty.");

        public static readonly Error NoSeries = Error.Validation(
            "Chart.NoSeries",
            "A chart must contain at least one data series.");

        public static readonly Error EmptySeries = Error.Validation(
            "Chart.EmptySeries",
            "A chart series must contain at least one value.");

        public static readonly Error EmptySeriesName = Error.Validation(
            "Chart.EmptySeriesName",
            "A chart series must have a name.");

        public static readonly Error SeriesLengthMismatch = Error.Validation(
            "Chart.SeriesLengthMismatch",
            "Every series must supply exactly one value per category.");

        public static readonly Error NonPositiveDimension = Error.Validation(
            "Chart.NonPositiveDimension",
            "Chart width and height must be greater than zero.");

        public static readonly Error PieRequiresSingleSeries = Error.Validation(
            "Chart.PieRequiresSingleSeries",
            "A pie chart must contain exactly one series.");

        public static readonly Error PieRequiresNonNegativeValues = Error.Validation(
            "Chart.PieRequiresNonNegativeValues",
            "A pie chart's values must not be negative.");
    }

    public static class Grid
    {
        public static readonly Error ColumnWidthOutOfRange = Error.Validation(
            "Grid.ColumnWidthOutOfRange",
            "A column width must be between 1 and 12.");

        public static readonly Error RowOverflow = Error.Validation(
            "Grid.RowOverflow",
            "The combined width of a row's columns must not exceed 12.");

        public static readonly Error NoColumns = Error.Validation(
            "Grid.NoColumns",
            "A row must contain at least one column.");

        public static readonly Error EmptyColumn = Error.Validation(
            "Grid.EmptyColumn",
            "A column must contain at least one block of content.");

        public static readonly Error NoSpaceForAutoColumns = Error.Validation(
            "Grid.NoSpaceForAutoColumns",
            "Auto-width columns need at least one free grid unit to share.");
    }
}
