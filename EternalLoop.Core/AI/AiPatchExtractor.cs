namespace EternalLoop.Core.AI;

public sealed class AiPatchExtractor
{
    public IReadOnlyList<float[][]> ExtractPatches(
        IReadOnlyList<float[]> melSpectrogram,
        int melBands,
        int patchFrames,
        int patchHopFrames)
    {
        ArgumentNullException.ThrowIfNull(melSpectrogram);

        if (melBands <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(melBands));
        }

        if (patchFrames <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(patchFrames));
        }

        if (patchHopFrames <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(patchHopFrames));
        }

        if (melSpectrogram.Count == 0)
        {
            return [];
        }

        ValidateFrames(melSpectrogram, melBands);

        var patches = new List<float[][]>();

        for (var startFrame = 0; startFrame < melSpectrogram.Count; startFrame += patchHopFrames)
        {
            patches.Add(CreatePatch(melSpectrogram, startFrame, melBands, patchFrames));
        }

        return patches;
    }

    private static void ValidateFrames(IReadOnlyList<float[]> melSpectrogram, int melBands)
    {
        for (var frameIndex = 0; frameIndex < melSpectrogram.Count; frameIndex++)
        {
            if (melSpectrogram[frameIndex] is null || melSpectrogram[frameIndex].Length != melBands)
            {
                throw new ArgumentException($"Mel spectrogram frame '{frameIndex}' must contain '{melBands}' bands.", nameof(melSpectrogram));
            }
        }
    }

    private static float[][] CreatePatch(
        IReadOnlyList<float[]> melSpectrogram,
        int startFrame,
        int melBands,
        int patchFrames)
    {
        var patch = new float[melBands][];

        for (var melBandIndex = 0; melBandIndex < melBands; melBandIndex++)
        {
            patch[melBandIndex] = new float[patchFrames];
        }

        for (var frameOffset = 0; frameOffset < patchFrames; frameOffset++)
        {
            var sourceFrameIndex = Math.Min(startFrame + frameOffset, melSpectrogram.Count - 1);
            var sourceFrame = melSpectrogram[sourceFrameIndex];

            for (var melBandIndex = 0; melBandIndex < melBands; melBandIndex++)
            {
                patch[melBandIndex][frameOffset] = Sanitize(sourceFrame[melBandIndex]);
            }
        }

        return patch;
    }

    private static float Sanitize(float value)
    {
        return float.IsFinite(value) ? value : 0.0f;
    }
}
