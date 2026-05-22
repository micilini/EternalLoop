namespace EternalLoop.Core.Audio;

public sealed class AudioLoaderOptions
{
    public TimeSpan MaxDuration { get; init; } = TimeSpan.FromMinutes(30);

    public int ReadBufferSize { get; init; } = 16_384;
}
