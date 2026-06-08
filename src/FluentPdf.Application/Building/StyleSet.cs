using FluentPdf.Domain.Styling;

namespace FluentPdf.Application.Building;

/// <summary>
/// A named palette of reusable <see cref="TextStyle"/>s. Define the document's styles once and
/// reference them by name from every block, so the whole document can be re-themed from a
/// single place instead of each component reinventing its own static style class.
/// </summary>
/// <example>
/// <code>
/// var styles = StyleSet.Create(set => set
///     .Define("title", s => s.Size(16).Bold().Color(navy))
///     .Define("muted", "title", s => s.Size(9).Bold(false).Color(slate)));
///
/// builder.Heading("Summary", styles["title"], spacingAfter: 6d);
/// </code>
/// </example>
public sealed class StyleSet
{
    private readonly IReadOnlyDictionary<string, TextStyle> _styles;

    private StyleSet(IReadOnlyDictionary<string, TextStyle> styles) => _styles = styles;

    /// <summary>Composes a palette fluently.</summary>
    public static StyleSet Create(Action<StyleSetBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new StyleSetBuilder();
        configure(builder);
        return builder.Build();
    }

    /// <summary>The style registered under <paramref name="name"/>.</summary>
    /// <exception cref="KeyNotFoundException">No style is registered under that name.</exception>
    public TextStyle this[string name] => Get(name);

    /// <summary>The style registered under <paramref name="name"/>.</summary>
    /// <exception cref="KeyNotFoundException">No style is registered under that name.</exception>
    public TextStyle Get(string name) =>
        _styles.TryGetValue(name, out var style)
            ? style
            : throw new KeyNotFoundException($"No text style named '{name}' is defined in this style set.");

    /// <summary>Looks up a style without throwing when it is absent.</summary>
    public bool TryGet(string name, out TextStyle style)
    {
        if (_styles.TryGetValue(name, out var found))
        {
            style = found;
            return true;
        }

        style = TextStyle.Default;
        return false;
    }

    /// <summary>Whether a style is registered under <paramref name="name"/>.</summary>
    public bool Contains(string name) => _styles.ContainsKey(name);

    internal static StyleSet FromStyles(IReadOnlyDictionary<string, TextStyle> styles) => new(styles);
}

/// <summary>Fluent builder for a <see cref="StyleSet"/>.</summary>
public sealed class StyleSetBuilder
{
    private readonly Dictionary<string, TextStyle> _styles = new(StringComparer.Ordinal);

    /// <summary>Registers a prebuilt style under <paramref name="name"/>.</summary>
    public StyleSetBuilder Define(string name, TextStyle style)
    {
        _styles[name] = style;
        return this;
    }

    /// <summary>Registers a style configured fluently from the default style.</summary>
    public StyleSetBuilder Define(string name, Action<TextStyleBuilder> configure)
    {
        var builder = new TextStyleBuilder();
        configure(builder);
        return Define(name, builder.Build());
    }

    /// <summary>
    /// Registers a style derived from an already-defined one, so variants (a coloured or larger
    /// version of a base style) share a single source of truth.
    /// </summary>
    public StyleSetBuilder Define(string name, string basedOn, Action<TextStyleBuilder> configure)
    {
        var baseStyle = _styles.TryGetValue(basedOn, out var existing) ? existing : TextStyle.Default;
        var builder = new TextStyleBuilder(baseStyle);
        configure(builder);
        return Define(name, builder.Build());
    }

    internal StyleSet Build() =>
        StyleSet.FromStyles(new Dictionary<string, TextStyle>(_styles, StringComparer.Ordinal));
}
