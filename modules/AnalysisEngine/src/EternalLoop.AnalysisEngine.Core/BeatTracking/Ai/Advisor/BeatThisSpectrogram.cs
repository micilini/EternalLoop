namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai.Advisor;

public sealed class BeatThisSpectrogram
{
    public BeatThisSpectrogram(
        float[] data,
        int frameCount,
        int melBins,
        double frameRate,
        double durationSeconds)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (frameCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameCount));
        }

        if (melBins <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(melBins));
        }

        if (frameRate <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameRate));
        }

        if (data.Length != frameCount * melBins)
        {
            throw new ArgumentException("Spectrogram data length must be frameCount * melBins.", nameof(data));
        }

        Data = data;
        FrameCount = frameCount;
        MelBins = melBins;
        FrameRate = frameRate;
        DurationSeconds = durationSeconds;
    }

    public float[] Data { get; }

    public int FrameCount { get; }

    public int MelBins { get; }

    public double FrameRate { get; }

    public double DurationSeconds { get; }

    public int GetOffset(int frameIndex, int melBin)
    {
        if (frameIndex < 0 || frameIndex >= FrameCount)
        {
            throw new ArgumentOutOfRangeException(nameof(frameIndex));
        }

        if (melBin < 0 || melBin >= MelBins)
        {
            throw new ArgumentOutOfRangeException(nameof(melBin));
        }

        return (frameIndex * MelBins) + melBin;
    }
}
