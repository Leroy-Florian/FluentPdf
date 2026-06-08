using FluentPdf.Kernel;

namespace FluentPdf.Domain.Styling;

/// <summary>
/// The visual styling of a run of text: font family, size, colour and decorations.
/// </summary>
public sealed class TextStyle : ValueObject
{
    /// <summary>The conventional default body style: 11pt black Helvetica.</summary>
    public const string DefaultFontFamily = "Helvetica";

    /// <summary>The conventional default font size, in points.</summary>
    public const double DefaultFontSize = 11d;

    private TextStyle(
        string fontFamily,
        double fontSize,
        Color color,
        bool isBold,
        bool isItalic,
        bool isUnderlined)
    {
        FontFamily = fontFamily;
        FontSize = fontSize;
        Color = color;
        IsBold = isBold;
        IsItalic = isItalic;
        IsUnderlined = isUnderlined;
    }

    public string FontFamily { get; }

    public double FontSize { get; }

    public Color Color { get; }

    public bool IsBold { get; }

    public bool IsItalic { get; }

    public bool IsUnderlined { get; }

    /// <summary>The default text style (11pt black Helvetica, no decorations).</summary>
    public static TextStyle Default { get; } =
        new(DefaultFontFamily, DefaultFontSize, Color.Black, false, false, false);

    /// <summary>Creates a text style, validating the font family and size.</summary>
    public static Result<TextStyle> Create(
        string fontFamily,
        double fontSize,
        Color color,
        bool isBold = false,
        bool isItalic = false,
        bool isUnderlined = false)
    {
        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            return DomainErrors.TextStyle.EmptyFontFamily;
        }

        if (fontSize <= 0d)
        {
            return DomainErrors.TextStyle.NonPositiveFontSize;
        }

        if (color is null)
        {
            return Error.NullValue;
        }

        return new TextStyle(fontFamily.Trim(), fontSize, color, isBold, isItalic, isUnderlined);
    }

    /// <summary>Returns a copy of this style with the given font family.</summary>
    public TextStyle WithFontFamily(string fontFamily) =>
        new(fontFamily, FontSize, Color, IsBold, IsItalic, IsUnderlined);

    /// <summary>Returns a copy of this style with the given font size.</summary>
    public TextStyle WithFontSize(double fontSize) =>
        new(FontFamily, fontSize, Color, IsBold, IsItalic, IsUnderlined);

    /// <summary>Returns a copy of this style with the given colour.</summary>
    public TextStyle WithColor(Color color) =>
        new(FontFamily, FontSize, color, IsBold, IsItalic, IsUnderlined);

    /// <summary>Returns a copy of this style with bold enabled or disabled.</summary>
    public TextStyle WithBold(bool isBold = true) =>
        new(FontFamily, FontSize, Color, isBold, IsItalic, IsUnderlined);

    /// <summary>Returns a copy of this style with italic enabled or disabled.</summary>
    public TextStyle WithItalic(bool isItalic = true) =>
        new(FontFamily, FontSize, Color, IsBold, isItalic, IsUnderlined);

    /// <summary>Returns a copy of this style with underline enabled or disabled.</summary>
    public TextStyle WithUnderline(bool isUnderlined = true) =>
        new(FontFamily, FontSize, Color, IsBold, IsItalic, isUnderlined);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FontFamily;
        yield return FontSize;
        yield return Color;
        yield return IsBold;
        yield return IsItalic;
        yield return IsUnderlined;
    }
}
