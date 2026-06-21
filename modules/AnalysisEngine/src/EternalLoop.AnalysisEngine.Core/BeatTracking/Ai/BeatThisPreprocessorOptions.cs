namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;

public sealed class BeatThisPreprocessorOptions
{
    public int SampleRate { get; init; } = 22_050;

    public double FrameRate { get; init; } = 50.0;

    public int ChunkFrames { get; init; } = 1_500;

    public int MelBins { get; init; } = 128;

    public int FrameSize { get; init; } = 1_024;

    public double MinFrequency { get; init; } = 20.0;

    public double? MaxFrequency { get; init; }

    public double LogEpsilon { get; init; } = 1e-6;

    public bool Normalize { get; init; } = true;

    public static BeatThisPreprocessorOptions FromMetadata(BeatThisModelMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return new BeatThisPreprocessorOptions
        {
            SampleRate = metadata.SampleRate,
            FrameRate = metadata.FrameRate,
            ChunkFrames = metadata.ChunkFrames,
            MelBins = metadata.MelBins,
            FrameSize = metadata.FrameSize
        };
    }
}
