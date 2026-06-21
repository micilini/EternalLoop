namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai.Advisor;

public sealed class BeatThisOfficialAggregateOptions
{
    public int ChunkFrames { get; init; } = 1_500;

    public int BorderFrames { get; init; } = 6;

    public int MelBins { get; init; } = 128;

    public int HopFrames => ChunkFrames - (2 * BorderFrames);

    public int FirstStartFrame => -BorderFrames;

    public string AggregatePolicy { get; init; } = "keep_first";

    public void Validate()
    {
        if (ChunkFrames <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ChunkFrames));
        }

        if (BorderFrames < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(BorderFrames));
        }

        if (MelBins <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MelBins));
        }

        if (HopFrames <= 0)
        {
            throw new InvalidDataException("Official aggregate hop must be positive.");
        }

        if (!string.Equals(AggregatePolicy, "keep_first", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Unsupported aggregate strategy. Expected keep_first.");
        }
    }
}
