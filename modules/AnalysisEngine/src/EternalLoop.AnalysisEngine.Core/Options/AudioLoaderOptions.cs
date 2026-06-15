namespace EternalLoop.AnalysisEngine.Core.Options;

public sealed class AudioLoaderOptions
{
    public const int DefaultReadBufferSize = 16_384;

    public static readonly TimeSpan DefaultMaxDuration = TimeSpan.FromMinutes(30);

    public TimeSpan MaxDuration { get; init; } = DefaultMaxDuration;

    public int ReadBufferSize { get; init; } = DefaultReadBufferSize;
}
