using System;
using System.IO;
using System.Text;

namespace EternalLoop.App.Services;

internal static class Mp4ArtworkReader
{
    private const int MaxAtomDepth = 16;
    private const int HeaderSize = 8; // 4-byte size + 4-byte type

    public static byte[]? TryReadArtwork(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        try
        {
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            return FindArtwork(stream);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static byte[]? FindArtwork(Stream stream)
    {
        return TraverseAtoms(stream, stream.Length, 0);
    }

    private static byte[]? TraverseAtoms(Stream stream, long boundary, int depth)
    {
        if (depth > MaxAtomDepth)
        {
            return null;
        }

        while (stream.Position + HeaderSize <= boundary)
        {
            long atomStart = stream.Position;
            Span<byte> header = stackalloc byte[HeaderSize];
            if (!TryReadExact(stream, header))
            {
                return null;
            }

            uint size = ReadBigEndian32(header[0..4]);
            string type = Encoding.ASCII.GetString(header[4..8]);

            long atomEnd;
            if (size == 0)
            {
                atomEnd = boundary;
            }
            else if (size == 1)
            {
                Span<byte> extendedSize = stackalloc byte[8];
                if (!TryReadExact(stream, extendedSize))
                {
                    return null;
                }
                long size64 = ReadBigEndian64(extendedSize);
                atomEnd = atomStart + size64;
            }
            else
            {
                atomEnd = atomStart + size;
            }

            if (atomEnd < atomStart + HeaderSize || atomEnd > boundary)
            {
                return null;
            }

            if (type == "covr")
            {
                // Traverse children to find 'data'
                return FindDataInCovr(stream, atomEnd, depth + 1);
            }

            if (IsContainer(type))
            {
                long containerStart = stream.Position;
                // Special case for 'meta': skip 4-byte version/flags
                if (type == "meta")
                {
                    if (stream.Position + 4 <= atomEnd)
                    {
                        stream.Position += 4;
                        containerStart = stream.Position;
                    }
                }

                byte[]? result = TraverseAtoms(stream, atomEnd, depth + 1);
                if (result != null) return result;
                stream.Position = atomEnd;
            }
            else
            {
                stream.Position = atomEnd;
            }
        }

        return null;
    }

    private static byte[]? FindDataInCovr(Stream stream, long boundary, int depth)
    {
        while (stream.Position + HeaderSize <= boundary)
        {
            long atomStart = stream.Position;
            Span<byte> header = stackalloc byte[HeaderSize];
            if (!TryReadExact(stream, header)) return null;

            uint size = ReadBigEndian32(header[0..4]);
            string type = Encoding.ASCII.GetString(header[4..8]);

            long atomEnd = atomStart + size;
            if (atomEnd > boundary) return null;

            if (type == "data")
            {
                // Found data! skip 8 bytes (4 bytes type/flags, 4 bytes locale/reserved)
                if (stream.Position + 8 <= atomEnd)
                {
                    stream.Position += 8;
                    int payloadSize = (int)(atomEnd - stream.Position);
                    if (payloadSize > 0)
                    {
                        byte[] payload = new byte[payloadSize];
                        if (TryReadExact(stream, payload) && EmbeddedArtworkImageValidator.IsSupportedImage(payload))
                        {
                            return payload;
                        }
                    }
                }
                return null;
            }
            stream.Position = atomEnd;
        }
        return null;
    }

    private static bool IsContainer(string type)
    {
        return type == "moov" || type == "udta" || type == "meta" || type == "ilst";
    }

    private static uint ReadBigEndian32(ReadOnlySpan<byte> bytes)
    {
        return ((uint)bytes[0] << 24)
            | ((uint)bytes[1] << 16)
            | ((uint)bytes[2] << 8)
            | bytes[3];
    }

    private static long ReadBigEndian64(ReadOnlySpan<byte> bytes)
    {
        return ((long)bytes[0] << 56)
            | ((long)bytes[1] << 48)
            | ((long)bytes[2] << 40)
            | ((long)bytes[3] << 32)
            | ((long)bytes[4] << 24)
            | ((long)bytes[5] << 16)
            | ((long)bytes[6] << 8)
            | bytes[7];
    }

    private static bool TryReadExact(Stream stream, Span<byte> buffer)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = stream.Read(buffer[offset..]);
            if (read <= 0) return false;
            offset += read;
        }
        return true;
    }
}
