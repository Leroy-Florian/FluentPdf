using System.Globalization;

namespace FluentPdf.Samples.FinancialReport;

/// <summary>
/// Culture-invariant formatting helpers shared by the report's presentation blocks, so every
/// figure is rendered identically regardless of the host machine's locale.
/// </summary>
internal static class ReportFormatting
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    /// <summary>Formats an amount with thousands separators and no decimals (e.g. 1,234).</summary>
    public static string Amount(decimal value) => value.ToString("#,##0", Culture);

    /// <summary>Formats an amount with an explicit sign (e.g. +1,234 / -1,234).</summary>
    public static string Signed(decimal value) =>
        (value < 0m ? "-" : "+") + Math.Abs(value).ToString("#,##0", Culture);

    /// <summary>Formats a percentage with one decimal and an explicit sign (e.g. +4.2%).</summary>
    public static string SignedPercent(double value) =>
        (value < 0d ? "-" : "+") + Math.Abs(value).ToString("0.0", Culture) + "%";

    /// <summary>Formats a plain percentage with one decimal (e.g. 12.4%).</summary>
    public static string Percent(double value) => value.ToString("0.0", Culture) + "%";

    /// <summary>Formats an integer count with thousands separators.</summary>
    public static string Count(int value) => value.ToString("#,##0", Culture);

    /// <summary>Formats a date in ISO-8601 form (e.g. 2025-12-31).</summary>
    public static string Date(DateOnly value) => value.ToString("yyyy-MM-dd", Culture);
}
