using System;

namespace EternalLoop.App.Services;

internal static class EmbeddedArtworkImageValidator
{
    private const int MaxImagePayloadSize = 8 * 1024 * 1024;

    public static bool IsSupportedImage(ReadOnlySpan<byte> imageBytes)
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
}
