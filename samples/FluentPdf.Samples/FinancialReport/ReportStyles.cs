using FluentPdf.Domain.Styling;

namespace FluentPdf.Samples.FinancialReport;

/// <summary>
/// The report's shared text-style palette. Centralising styles keeps every reusable block
/// visually consistent and lets the whole document be re-themed from one place.
/// </summary>
internal static class ReportStyles
{
    private static readonly Color Navy = Color.FromHex("#1F3864").Value;
    private static readonly Color Slate = Color.FromHex("#44546A").Value;
    private static readonly Color Positive = Color.FromHex("#2E7D32").Value;
    private static readonly Color Negative = Color.FromHex("#C62828").Value;

    public static TextStyle CoverTitle => TextStyle.Default.WithFontSize(28d).WithBold().WithColor(Navy);

    public static TextStyle CoverSubtitle => TextStyle.Default.WithFontSize(14d).WithColor(Slate);

    public static TextStyle SectionTitle => TextStyle.Default.WithFontSize(16d).WithBold().WithColor(Navy);

    public static TextStyle SubHeading => TextStyle.Default.WithFontSize(12d).WithBold().WithColor(Slate);

    public static TextStyle KpiValue => TextStyle.Default.WithFontSize(18d).WithBold().WithColor(Navy);

    public static TextStyle Muted => TextStyle.Default.WithFontSize(9d).WithColor(Slate);

    public static TextStyle Strong => TextStyle.Default.WithBold();

    public static TextStyle Favourable => TextStyle.Default.WithBold().WithColor(Positive);

    public static TextStyle Adverse => TextStyle.Default.WithBold().WithColor(Negative);
}
