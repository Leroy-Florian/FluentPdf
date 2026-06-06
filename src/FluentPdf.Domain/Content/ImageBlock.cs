using FluentPdf.Domain.Styling;
using FluentPdf.Kernel;

namespace FluentPdf.Domain.Content;

/// <summary>
/// An embedded raster image with an intrinsic size, in points. The raw encoded bytes are
/// carried verbatim so each adapter embeds them with its own library.
/// </summary>
public sealed class ImageBlock : IBlock
{
    private readonly byte[] _data;

    private ImageBlock(
        byte[] data,
        ImageFormat format,
        double width,
        double height,
        HorizontalAlignment alignment)
    {
        _data = data;
        Format = format;
        Width = width;
        Height = height;
        Alignment = alignment;
    }

    /// <summary>The raw encoded image bytes.</summary>
    public IReadOnlyList<byte> Data => _data;

    public ImageFormat Format { get; }

    public double Width { get; }

    public double Height { get; }

    public HorizontalAlignment Alignment { get; }

    /// <summary>Creates an image block from encoded bytes and an intrinsic size in points.</summary>
    public static Result<ImageBlock> Create(
        byte[] data,
        ImageFormat format,
        double width,
        double height,
        HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        if (data is null || data.Length == 0)
        {
            return DomainErrors.Image.EmptyData;
        }

        if (width <= 0d || height <= 0d)
        {
            return DomainErrors.Image.NonPositiveDimension;
        }

        // Defensive copy so the caller cannot mutate the image after validation.
        return new ImageBlock([.. data], format, width, height, alignment);
    }
}
