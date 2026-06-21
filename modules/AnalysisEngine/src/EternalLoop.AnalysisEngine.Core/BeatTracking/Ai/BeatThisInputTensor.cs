namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;

public sealed class BeatThisInputTensor
{
    public BeatThisInputTensor(
        float[] data,
        long[] shape,
        int validFrameCount,
        int sampleRate,
        double frameRate,
        int chunkFrames,
        int melBins,
        int frameSize,
        int hopSize,
        double durationSeconds,
        int startFrameIndex = 0,
        double startTimeSeconds = 0.0)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(shape);

        if (shape.Length != 3)
        {
            throw new ArgumentException("Beat This input tensor shape must have 3 dimensions.", nameof(shape));
        }

        if (startFrameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startFrameIndex));
        }

        if (startTimeSeconds < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(startTimeSeconds));
        }

        Data = data;
        Shape = shape;
        ValidFrameCount = validFrameCount;
        SampleRate = sampleRate;
        FrameRate = frameRate;
        ChunkFrames = chunkFrames;
        MelBins = melBins;
        FrameSize = frameSize;
        HopSize = hopSize;
        DurationSeconds = durationSeconds;
        StartFrameIndex = startFrameIndex;
        StartTimeSeconds = startTimeSeconds;
    }

    public float[] Data { get; }

    public long[] Shape { get; }

    public int ValidFrameCount { get; }

    public int SampleRate { get; }

    public double FrameRate { get; }

    public int ChunkFrames { get; }

    public int MelBins { get; }

    public int FrameSize { get; }

    public int HopSize { get; }

    public double DurationSeconds { get; }

    public int StartFrameIndex { get; }

    public double StartTimeSeconds { get; }

    public int GetOffset(int frameIndex, int melBin)
    {
        if (frameIndex < 0 || frameIndex >= ChunkFrames)
        {
            throw new ArgumentOutOfRangeException(nameof(frameIndex));
        }

        if (melBin < 0 || melBin >= MelBins)
        {
            throw new ArgumentOutOfRangeException(nameof(melBin));
        }

        return (frameIndex * MelBins) + melBin;
    }

    public double GetFrameTimeSeconds(int frameIndex)
    {
        if (frameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameIndex));
        }

        return StartTimeSeconds + (frameIndex / FrameRate);
    }
}
