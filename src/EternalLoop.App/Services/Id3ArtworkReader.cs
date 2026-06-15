using System.IO;

namespace EternalLoop.App.Services;

internal static class Id3ArtworkReader
{
    private const int TagHeaderSize = 10;
    private const int MaxTagSize = 16 * 1024 * 1024;
    private const int MaxImagePayloadSize = 8 * 1024 * 1024;

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

            return TryReadArtwork(stream);
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

    private static byte[]? TryReadArtwork(Stream stream)
    {
        Span<byte> header = stackalloc byte[TagHeaderSize];
        if (!TryReadExact(stream, header))
        {
            return null;
        }

        if (!header[..3].SequenceEqual("ID3"u8))
        {
            return null;
        }

        int versionMajor = header[3];
        if (versionMajor is < 2 or > 4)
        {
            return null;
        }

        int tagSize = ReadSynchsafeInt(header[6..10]);
        if (tagSize <= 0 || tagSize > MaxTagSize || tagSize > stream.Length - TagHeaderSize)
        {
            return null;
        }

        long tagEnd = TagHeaderSize + tagSize;

        if ((header[5] & 0x40) != 0)
        {
            if (!SkipExtendedHeader(stream, versionMajor, tagEnd))
            {
                return null;
            }
        }

        int frameHeaderSize = versionMajor == 2 ? 6 : 10;
        ArtworkCandidate? preferredCandidate = null;
        ArtworkCandidate? fallbackCandidate = null;
        byte[] frameHeaderBuffer = new byte[10];

        while (stream.Position + frameHeaderSize <= tagEnd)
        {
            Span<byte> frameHeader = frameHeaderBuffer.AsSpan(0, frameHeaderSize);

            if (!TryReadExact(stream, frameHeader))
            {
                return null;
            }

            if (IsPadding(frameHeader))
            {
                break;
            }

            int frameSize = versionMajor == 2
                ? ReadBigEndian24(frameHeader[3..6])
                : versionMajor == 3
                    ? ReadBigEndian32(frameHeader[4..8])
                    : ReadSynchsafeInt(frameHeader[4..8]);

            if (frameSize <= 0)
            {
                continue;
            }

            if (stream.Position + frameSize > tagEnd)
            {
                return null;
            }

            byte[] payload = new byte[frameSize];
            if (!TryReadExact(stream, payload))
            {
                return null;
            }

            ArtworkCandidate? candidate = versionMajor == 2
                ? TryParsePicFrame(frameHeader[..3], payload)
                : TryParseApicFrame(frameHeader[..4], payload);

            if (candidate is null)
            {
                continue;
            }

            if (candidate.PictureType == 3 && preferredCandidate is null)
            {
                preferredCandidate = candidate;
            }

            fallbackCandidate ??= candidate;
        }

        return preferredCandidate?.ImageBytes ?? fallbackCandidate?.ImageBytes;
    }

    private static bool SkipExtendedHeader(Stream stream, int versionMajor, long tagEnd)
    {
        Span<byte> sizeBytes = stackalloc byte[4];
        if (!TryReadExact(stream, sizeBytes))
        {
            return false;
        }

        int extendedSize = versionMajor == 4
            ? ReadSynchsafeInt(sizeBytes)
            : ReadBigEndian32(sizeBytes);

        if (extendedSize <= 0 || stream.Position + extendedSize > tagEnd)
        {
            return false;
        }

        stream.Position += extendedSize;
        return true;
    }

    private static ArtworkCandidate? TryParseApicFrame(ReadOnlySpan<byte> frameId, ReadOnlySpan<byte> payload)
    {
        if (!frameId.SequenceEqual("APIC"u8) || payload.Length < 4)
        {
            return null;
        }

        byte encoding = payload[0];
        int mimeEnd = FindSingleTerminator(payload, 1);
        if (mimeEnd < 0 || mimeEnd + 2 > payload.Length)
        {
            return null;
        }

        int pictureTypeIndex = mimeEnd + 1;
        byte pictureType = payload[pictureTypeIndex];
        int descriptionStart = pictureTypeIndex + 1;
        int imageStart = FindDescriptionEnd(payload, descriptionStart, encoding);
        if (imageStart < 0 || imageStart >= payload.Length)
        {
            return null;
        }

        byte[] imageBytes = payload[imageStart..].ToArray();
        return IsSupportedImage(imageBytes)
            ? new ArtworkCandidate(pictureType, imageBytes)
            : null;
    }

    private static ArtworkCandidate? TryParsePicFrame(ReadOnlySpan<byte> frameId, ReadOnlySpan<byte> payload)
    {
        if (!frameId.SequenceEqual("PIC"u8) || payload.Length < 6)
        {
            return null;
        }

        byte encoding = payload[0];
        int descriptionStart = 5;
        byte pictureType = payload[4];
        int imageStart = FindDescriptionEnd(payload, descriptionStart, encoding);
        if (imageStart < 0 || imageStart >= payload.Length)
        {
            return null;
        }

        byte[] imageBytes = payload[imageStart..].ToArray();
        return IsSupportedImage(imageBytes)
            ? new ArtworkCandidate(pictureType, imageBytes)
            : null;
    }

    private static int FindDescriptionEnd(ReadOnlySpan<byte> payload, int start, byte encoding)
    {
        if (start >= payload.Length)
        {
            return -1;
        }

        if (encoding is 1 or 2)
        {
            for (int index = start; index + 1 < payload.Length; index++)
            {
                if (payload[index] == 0 && payload[index + 1] == 0)
                {
                    return index + 2;
                }
            }

            return -1;
        }

        int terminator = FindSingleTerminator(payload, start);
        return terminator < 0 ? -1 : terminator + 1;
    }

    private static int FindSingleTerminator(ReadOnlySpan<byte> payload, int start)
    {
        for (int index = start; index < payload.Length; index++)
        {
            if (payload[index] == 0)
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsSupportedImage(ReadOnlySpan<byte> imageBytes)
    {
        if (imageBytes.Length < 3 || imageBytes.Length > MaxImagePayloadSize)
        {
            return false;
        }

        return StartsWith(imageBytes, 0xFF, 0xD8, 0xFF)
            || StartsWith(imageBytes, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)
            || StartsWith(imageBytes, 0x42, 0x4D)
            || StartsWith(imageBytes, 0x47, 0x49, 0x46, 0x38)
            || StartsWith(imageBytes, 0x49, 0x49, 0x2A, 0x00)
            || StartsWith(imageBytes, 0x4D, 0x4D, 0x00, 0x2A);
    }

    private static bool StartsWith(ReadOnlySpan<byte> bytes, params byte[] prefix)
    {
        return bytes.Length >= prefix.Length && bytes[..prefix.Length].SequenceEqual(prefix);
    }

    private static bool IsPadding(ReadOnlySpan<byte> frameHeader)
    {
        for (int index = 0; index < frameHeader.Length; index++)
        {
            if (frameHeader[index] != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static int ReadSynchsafeInt(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] & 0x7F) << 21
            | (bytes[1] & 0x7F) << 14
            | (bytes[2] & 0x7F) << 7
            | (bytes[3] & 0x7F);
    }

    private static int ReadBigEndian32(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] << 24)
            | (bytes[1] << 16)
            | (bytes[2] << 8)
            | bytes[3];
    }

    private static int ReadBigEndian24(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] << 16)
            | (bytes[1] << 8)
            | bytes[2];
    }

    private static bool TryReadExact(Stream stream, Span<byte> buffer)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = stream.Read(buffer[offset..]);
            if (read <= 0)
            {
                return false;
            }

            offset += read;
        }

        return true;
    }

    private static bool TryReadExact(Stream stream, byte[] buffer)
    {
        return TryReadExact(stream, buffer.AsSpan());
    }

    private sealed record ArtworkCandidate(byte PictureType, byte[] ImageBytes);
}
