namespace EternalLoop.AnalysisEngine.Core.Features;

internal static class ChromaExtractor
{
    public const int PitchClassCount = 12;

    private const int MedianFilterWindow = 5;
    private const double MinimumChromaFrequency = 50.0;
    private const double MaximumChromaFrequency = 5000.0;
    private const double ReferencePitchFrequency = 440.0;
    private const double PitchClassesPerOctave = 12.0;
    private const double ReferencePitchClassOffset = 9.0;
    private const int StackAllocatedMedianLimit = 16;

    public static float[][] Compute(StftFrame[] frames, int sampleRate, int frameSize)
    {
        ArgumentNullException.ThrowIfNull(frames);

        if (frames.Length == 0)
        {
            return [];
        }

        var chroma = new float[frames.Length][];

        for (var frameIndex = 0; frameIndex < frames.Length; frameIndex++)
        {
            chroma[frameIndex] = MapToChroma(frames[frameIndex].Magnitudes, sampleRate, frameSize);
        }

        return ApplyMedianFilter(chroma, MedianFilterWindow);
    }

    private static float[] MapToChroma(float[] magnitudes, int sampleRate, int frameSize)
    {
        var chroma = new float[PitchClassCount];

        for (var bin = 1; bin < magnitudes.Length; bin++)
        {
            var frequency = bin * sampleRate / (double)frameSize;

            if (frequency < MinimumChromaFrequency || frequency > MaximumChromaFrequency)
            {
                continue;
            }

            var pitchClass = (int)Math.Round(
                (PitchClassesPerOctave * Math.Log2(frequency / ReferencePitchFrequency)) +
                ReferencePitchClassOffset) % PitchClassCount;

            if (pitchClass < 0)
            {
                pitchClass += PitchClassCount;
            }

            chroma[pitchClass] += magnitudes[bin];
        }

        NormalizeInPlace(chroma);
        return chroma;
    }

    private static float[][] ApplyMedianFilter(float[][] chroma, int windowSize)
    {
        if (chroma.Length == 0)
        {
            return [];
        }

        var safeWindow = Math.Max(1, windowSize);

        if (safeWindow % 2 == 0)
        {
            safeWindow++;
        }

        var radius = safeWindow / 2;
        var filtered = new float[chroma.Length][];

        for (var frame = 0; frame < chroma.Length; frame++)
        {
            filtered[frame] = new float[PitchClassCount];
            var start = Math.Max(0, frame - radius);
            var end = Math.Min(chroma.Length - 1, frame + radius);

            for (var pitchClass = 0; pitchClass < PitchClassCount; pitchClass++)
            {
                filtered[frame][pitchClass] = Median(chroma, start, end, pitchClass);
            }

            NormalizeInPlace(filtered[frame]);
        }

        return filtered;
    }

    private static float Median(float[][] values, int startFrame, int endFrame, int pitchClass)
    {
        var count = endFrame - startFrame + 1;
        Span<float> buffer = count <= StackAllocatedMedianLimit
            ? stackalloc float[count]
            : new float[count];

        for (var index = 0; index < count; index++)
        {
            buffer[index] = values[startFrame + index][pitchClass];
        }

        buffer.Sort();
        var middle = count / 2;

        if (count % 2 == 1)
        {
            return buffer[middle];
        }

        return (buffer[middle - 1] + buffer[middle]) * 0.5f;
    }

    private static void NormalizeInPlace(float[] vector)
    {
        var max = vector.Max();

        if (max <= 0.0f)
        {
            return;
        }

        for (var index = 0; index < vector.Length; index++)
        {
            vector[index] /= max;
        }
    }
}
