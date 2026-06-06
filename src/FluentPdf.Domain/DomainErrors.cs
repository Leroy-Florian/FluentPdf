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
}
