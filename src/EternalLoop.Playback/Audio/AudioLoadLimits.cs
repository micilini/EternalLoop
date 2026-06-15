namespace EternalLoop.Playback.Audio;

public sealed class AudioLoadLimits
{
    public static AudioLoadLimits Default { get; } = new();

    public TimeSpan MaxDuration { get; init; } = TimeSpan.FromMinutes(20);

    public long MaxDecodedSamples { get; init; } = 60L * 60L * 48_000L * 2L;

    public long MaxDecodedBytes => checked(MaxDecodedSamples * sizeof(float));
}
