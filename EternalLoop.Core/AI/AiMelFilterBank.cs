using EternalLoop.Core.Analysis;

namespace EternalLoop.Core.AI;

public sealed class AiMelFilterBank
{
    public double[][] Create(
        int sampleRate,
        int fftSize,
        int melBands,
        double minFrequencyHertz)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        if (fftSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fftSize));
        }

        if (melBands <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(melBands));
        }

        if (minFrequencyHertz < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(minFrequencyHertz));
        }

        return MelScale.CreateFilterBank(sampleRate, fftSize, melBands, minFrequencyHertz);
    }
}
