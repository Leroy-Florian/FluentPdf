using FluentPdf.Domain.Styling;

namespace FluentPdf.Application.Building;

/// <summary>
/// Fluent builder for a <see cref="TextStyle"/>. Starts from the default body style (or an
/// explicit base) and applies decorations one chained call at a time, so callers stop writing
/// long <c>TextStyle.Default.WithFontSize(..).WithBold().WithColor(..)</c> expressions by hand.
/// </summary>
public sealed class TextStyleBuilder
{
    private TextStyle _style;

    /// <summary>Starts from the default body style (11pt black Helvetica).</summary>
    public TextStyleBuilder()
        : this(TextStyle.Default)
    {
    }

    /// <summary>Starts from an explicit base style, e.g. to derive a variant.</summary>
    public TextStyleBuilder(TextStyle baseStyle) => _style = baseStyle ?? TextStyle.Default;

    /// <summary>Sets the font family.</summary>
    public TextStyleBuilder Font(string family)
    {
        _style = _style.WithFontFamily(family);
        return this;
    }

    /// <summary>Sets the font size, in points.</summary>
    public TextStyleBuilder Size(double points)
    {
        _style = _style.WithFontSize(points);
        return this;
    }

    /// <summary>Sets the text colour.</summary>
    public TextStyleBuilder Color(Color color)
    {
        _style = _style.WithColor(color);
        return this;
    }

    /// <summary>Enables or disables bold.</summary>
    public TextStyleBuilder Bold(bool on = true)
    {
        _style = _style.WithBold(on);
        return this;
    }

    /// <summary>Enables or disables italic.</summary>
    public TextStyleBuilder Italic(bool on = true)
    {
        _style = _style.WithItalic(on);
        return this;
    }

    /// <summary>Enables or disables underline.</summary>
    public TextStyleBuilder Underline(bool on = true)
    {
        _style = _style.WithUnderline(on);
        return this;
    }

    /// <summary>Returns the configured style.</summary>
    public TextStyle Build() => _style;
}
