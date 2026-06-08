using System.Reflection;

namespace FluentPdf.Adapters.Shared;

/// <summary>
/// The Liberation Sans font (SIL Open Font License), embedded so both real adapters render
/// text with identical glyph metrics. Using the same font is what lets the QuestPDF and iText
/// outputs line-break and paginate the same way — the foundation of consistent rendering.
/// </summary>
public static class EmbeddedFonts
{
    /// <summary>The font family name registered/used by the adapters.</summary>
    public const string Family = "Liberation Sans";

    private static readonly Lazy<byte[]> RegularBytes = Load("LiberationSans-Regular.ttf");
    private static readonly Lazy<byte[]> BoldBytes = Load("LiberationSans-Bold.ttf");
    private static readonly Lazy<byte[]> ItalicBytes = Load("LiberationSans-Italic.ttf");
    private static readonly Lazy<byte[]> BoldItalicBytes = Load("LiberationSans-BoldItalic.ttf");

    public static byte[] Regular => RegularBytes.Value;

    public static byte[] Bold => BoldBytes.Value;

    public static byte[] Italic => ItalicBytes.Value;

    public static byte[] BoldItalic => BoldItalicBytes.Value;

    /// <summary>The font bytes for a given weight/slant combination.</summary>
    public static byte[] For(bool bold, bool italic) => (bold, italic) switch
    {
        (true, true) => BoldItalic,
        (true, false) => Bold,
        (false, true) => Italic,
        _ => Regular,
    };

    private static Lazy<byte[]> Load(string fileName) => new(() =>
    {
        var assembly = typeof(EmbeddedFonts).Assembly;
        var resource = $"FluentPdf.Adapters.Shared.Fonts.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded font '{resource}' was not found.");

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    });
}
