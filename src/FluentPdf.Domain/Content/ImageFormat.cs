namespace FluentPdf.Domain.Content;

/// <summary>The encoding of an embedded image. Restricted to formats every PDF library
/// can embed without re-encoding.</summary>
public enum ImageFormat
{
    Png = 0,
    Jpeg = 1,
}
