using FluentPdf.Kernel;

namespace FluentPdf.Application.Rendering;

/// <summary>
/// The output of a successful render: the encoded document bytes plus light metadata.
/// </summary>
public sealed class RenderedPdf : ValueObject
{
    /// <summary>The MIME type of a PDF document.</summary>
    public const string PdfContentType = "application/pdf";

    private readonly byte[] _content;

    private RenderedPdf(byte[] content, string contentType, int pageCount)
    {
        _content = content;
        ContentType = contentType;
        PageCount = pageCount;
    }

    /// <summary>The encoded document bytes.</summary>
    public IReadOnlyList<byte> Content => _content;

    /// <summary>The MIME content type, typically <see cref="PdfContentType"/>.</summary>
    public string ContentType { get; }

    /// <summary>The number of pages produced.</summary>
    public int PageCount { get; }

    /// <summary>Returns a fresh copy of the encoded bytes (safe to hand to a stream).</summary>
    public byte[] ToArray() => [.. _content];

    /// <summary>Creates a render result. Used by adapters when reporting success.</summary>
    public static Result<RenderedPdf> Create(
        byte[] content,
        int pageCount,
        string contentType = PdfContentType)
    {
        if (content is null || content.Length == 0)
        {
            return RenderErrors.EmptyOutput;
        }

        if (pageCount <= 0)
        {
            return RenderErrors.NonPositivePageCount;
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return RenderErrors.MissingContentType;
        }

        return new RenderedPdf([.. content], contentType, pageCount);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ContentType;
        yield return PageCount;

        foreach (var b in _content)
        {
            yield return b;
        }
    }
}
