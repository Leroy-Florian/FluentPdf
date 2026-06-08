namespace FluentPdf.Adapters.Shared;

/// <summary>
/// The shared visual specification both real adapters follow so their output looks the same:
/// table borders, header shading, padding and the chart colour palette. Centralising it keeps
/// QuestPDF and iText pixel-consistent.
/// </summary>
public static class RenderingTheme
{
    /// <summary>Table cell border colour (#DDDDDD).</summary>
    public static readonly (byte R, byte G, byte B) BorderColor = (0xDD, 0xDD, 0xDD);

    /// <summary>Table header background (#F2F2F2).</summary>
    public static readonly (byte R, byte G, byte B) HeaderBackground = (0xF2, 0xF2, 0xF2);

    /// <summary>Table cell border width, in points.</summary>
    public const float BorderWidth = 0.75f;

    /// <summary>Table cell padding, in points.</summary>
    public const float CellPadding = 5f;

    /// <summary>The categorical colour palette used by charts (hex, without '#').</summary>
    public static readonly string[] Palette =
    [
        "1F3864", // navy
        "2E86AB", // teal
        "E1A100", // amber
        "2E7D32", // green
        "C62828", // red
        "6A4C93", // violet
        "00897B", // cyan
        "8D6E63", // brown
    ];
}
