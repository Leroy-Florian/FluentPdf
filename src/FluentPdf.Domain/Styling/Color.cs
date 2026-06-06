using System.Globalization;
using FluentPdf.Kernel;

namespace FluentPdf.Domain.Styling;

/// <summary>
/// An RGBA colour. Components <see cref="Red"/>, <see cref="Green"/> and <see cref="Blue"/>
/// range from 0 to 255; <see cref="Alpha"/> ranges from 0 (transparent) to 1 (opaque).
/// </summary>
public sealed class Color : ValueObject
{
    private Color(byte red, byte green, byte blue, double alpha)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }

    public byte Red { get; }

    public byte Green { get; }

    public byte Blue { get; }

    public double Alpha { get; }

    /// <summary>Opaque black (#000000).</summary>
    public static Color Black { get; } = new(0, 0, 0, 1d);

    /// <summary>Opaque white (#FFFFFF).</summary>
    public static Color White { get; } = new(255, 255, 255, 1d);

    /// <summary>Creates an opaque colour from RGB components.</summary>
    public static Result<Color> FromRgb(int red, int green, int blue) =>
        FromRgba(red, green, blue, 1d);

    /// <summary>Creates a colour from RGB components and an alpha value.</summary>
    public static Result<Color> FromRgba(int red, int green, int blue, double alpha)
    {
        if (IsOutsideByteRange(red) || IsOutsideByteRange(green) || IsOutsideByteRange(blue))
        {
            return DomainErrors.Color.ComponentOutOfRange;
        }

        if (alpha is < 0d or > 1d)
        {
            return DomainErrors.Color.AlphaOutOfRange;
        }

        return new Color((byte)red, (byte)green, (byte)blue, alpha);
    }

    /// <summary>
    /// Parses a hex colour in the form <c>#RGB</c>, <c>#RRGGBB</c> or <c>#RRGGBBAA</c>.
    /// The leading <c>#</c> is optional.
    /// </summary>
    public static Result<Color> FromHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return DomainErrors.Color.InvalidHex;
        }

        var value = hex.Trim().TrimStart('#');

        // Expand the shorthand #RGB form to #RRGGBB.
        if (value.Length == 3)
        {
            value = string.Concat(value[0], value[0], value[1], value[1], value[2], value[2]);
        }

        if ((value.Length != 6 && value.Length != 8) || !IsHexDigits(value))
        {
            return DomainErrors.Color.InvalidHex;
        }

        var red = ParseByte(value, 0);
        var green = ParseByte(value, 2);
        var blue = ParseByte(value, 4);
        var alpha = value.Length == 8 ? ParseByte(value, 6) / 255d : 1d;

        return new Color(red, green, blue, alpha);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Red;
        yield return Green;
        yield return Blue;
        yield return Alpha;
    }

    private static bool IsOutsideByteRange(int value) => value is < 0 or > 255;

    private static bool IsHexDigits(string value)
    {
        foreach (var c in value)
        {
            var isHex = c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');

            if (!isHex)
            {
                return false;
            }
        }

        return true;
    }

    private static byte ParseByte(string value, int start) =>
        byte.Parse(value.Substring(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}
