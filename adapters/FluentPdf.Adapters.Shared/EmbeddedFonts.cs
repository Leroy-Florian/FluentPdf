using System.Reflection;

namespace FluentPdf.Adapters.Shared;

/// <summary>
/// The Liberation Sans font (SIL Open Font License), embedded so both real adapters render
/// text with identical glyph metrics. Only the regular and bold weights are shipped to keep
/// the package small; italics are synthesised (faux-italic) by the adapters when needed.
/// </summary>
public static class EmbeddedFonts
{
    /// <summary>The font family name registered/used by the adapters.</summary>
    public const string Family = "Liberation Sans";

    private static readonly Lazy<byte[]> RegularBytes = Load("LiberationSans-Regular.ttf");
    private static readonly Lazy<byte[]> BoldBytes = Load("LiberationSans-Bold.ttf");

    public static byte[] Regular => RegularBytes.Value;

    public static byte[] Bold => BoldBytes.Value;

    /// <summary>The font bytes for the requested weight.</summary>
    public static byte[] For(bool bold) => bold ? Bold : Regular;

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
