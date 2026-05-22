namespace EternalLoop.Core.Audio;

public static class AudioFormatDetector
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wav",
        ".mp3",
        ".flac",
        ".m4a",
        ".aac"
    };

    public static AudioFormatDetectionResult Detect(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return new AudioFormatDetectionResult(AudioFileFormat.Unknown, string.Empty, false);
        }

        var extension = Path.GetExtension(filePath);

        var format = DetectByMagicBytes(filePath);
        if (format == AudioFileFormat.Unknown)
        {
            format = DetectByExtension(extension);
        }

        return new AudioFormatDetectionResult(
            format,
            extension,
            format != AudioFileFormat.Unknown && SupportedExtensions.Contains(extension));
    }

    public static bool IsSupportedExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return SupportedExtensions.Contains(extension);
    }

    private static AudioFileFormat DetectByExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".wav" => AudioFileFormat.Wav,
            ".mp3" => AudioFileFormat.Mp3,
            ".flac" => AudioFileFormat.Flac,
            ".m4a" => AudioFileFormat.M4a,
            ".aac" => AudioFileFormat.Aac,
            _ => AudioFileFormat.Unknown
        };
    }

    private static AudioFileFormat DetectByMagicBytes(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return AudioFileFormat.Unknown;
        }

        Span<byte> header = stackalloc byte[16];

        try
        {
            using var stream = File.OpenRead(filePath);
            var read = stream.Read(header);

            if (read >= 12 &&
                header[0] == (byte)'R' &&
                header[1] == (byte)'I' &&
                header[2] == (byte)'F' &&
                header[3] == (byte)'F' &&
                header[8] == (byte)'W' &&
                header[9] == (byte)'A' &&
                header[10] == (byte)'V' &&
                header[11] == (byte)'E')
            {
                return AudioFileFormat.Wav;
            }

            if (read >= 3 &&
                header[0] == (byte)'I' &&
                header[1] == (byte)'D' &&
                header[2] == (byte)'3')
            {
                return AudioFileFormat.Mp3;
            }

            if (read >= 2 && header[0] == 0xFF && (header[1] & 0xE0) == 0xE0)
            {
                return AudioFileFormat.Mp3;
            }

            if (read >= 4 &&
                header[0] == (byte)'f' &&
                header[1] == (byte)'L' &&
                header[2] == (byte)'a' &&
                header[3] == (byte)'C')
            {
                return AudioFileFormat.Flac;
            }

            if (read >= 12 &&
                header[4] == (byte)'f' &&
                header[5] == (byte)'t' &&
                header[6] == (byte)'y' &&
                header[7] == (byte)'p')
            {
                return AudioFileFormat.M4a;
            }
        }
        catch (IOException)
        {
            return AudioFileFormat.Unknown;
        }
        catch (UnauthorizedAccessException)
        {
            return AudioFileFormat.Unknown;
        }

        return AudioFileFormat.Unknown;
    }
}
