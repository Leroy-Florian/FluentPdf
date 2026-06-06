using System.Buffers.Binary;
using System.IO.Compression;

namespace FluentPdf.Visual;

/// <summary>
/// A minimal, dependency-free PNG encoder (truecolour with alpha). Used to write rasterised
/// pages and diff heatmaps as artifacts for human review, without pulling in an imaging library.
/// </summary>
public static class PngImage
{
    private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];

    /// <summary>Encodes RGBA pixels (4 bytes per pixel) into a PNG byte array.</summary>
    public static byte[] EncodeRgba(int width, int height, byte[] rgba)
    {
        using var output = new MemoryStream();
        output.Write(Signature, 0, Signature.Length);

        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header[..4], width);
        BinaryPrimitives.WriteInt32BigEndian(header.Slice(4, 4), height);
        header[8] = 8; // bit depth
        header[9] = 6; // colour type: truecolour with alpha
        header[10] = 0; // compression
        header[11] = 0; // filter
        header[12] = 0; // interlace
        WriteChunk(output, "IHDR", header.ToArray());

        WriteChunk(output, "IDAT", Compress(width, height, rgba));
        WriteChunk(output, "IEND", []);

        return output.ToArray();
    }

    private static byte[] Compress(int width, int height, byte[] rgba)
    {
        var stride = width * 4;
        var raw = new byte[height * (stride + 1)];

        for (var row = 0; row < height; row++)
        {
            raw[row * (stride + 1)] = 0; // filter type: none
            Array.Copy(rgba, row * stride, raw, (row * (stride + 1)) + 1, stride);
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            zlib.Write(raw, 0, raw.Length);
        }

        return compressed.ToArray();
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        output.Write(length);

        var typeBytes = new byte[4];
        for (var i = 0; i < 4; i++)
        {
            typeBytes[i] = (byte)type[i];
        }

        output.Write(typeBytes, 0, 4);
        output.Write(data, 0, data.Length);

        var crc = Crc32.Compute(typeBytes, data);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        output.Write(crcBytes);
    }
}
