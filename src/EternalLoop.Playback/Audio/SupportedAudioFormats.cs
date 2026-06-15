namespace EternalLoop.Playback.Audio;

public static class SupportedAudioFormats
{
    public static IReadOnlySet<string> Extensions { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3",
            ".wav",
            ".m4a",
            ".aac"
        };

    public const string DisplayName = "MP3, WAV, M4A or AAC";

    public const string DialogFilter = "Audio files|*.mp3;*.wav;*.m4a;*.aac|All files|*.*";

    public static bool IsSupportedExtension(string pathOrExtension)
    {
        if (string.IsNullOrWhiteSpace(pathOrExtension))
        {
            return false;
        }

        string extension = Path.GetExtension(pathOrExtension);

        if (string.IsNullOrWhiteSpace(extension) && pathOrExtension.StartsWith('.'))
        {
            extension = pathOrExtension;
        }

        return Extensions.Contains(extension);
    }
}
