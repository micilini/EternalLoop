namespace EternalLoop.Playback.Audio;

public sealed class BranchTransitionSmoother
{
    public BranchTransitionSmoother(
        int sampleRate,
        int channels,
        BranchTransitionOptions? options = null)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        if (channels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(channels));
        }

        BranchTransitionOptions resolvedOptions = (options ?? new BranchTransitionOptions()).Normalize();
        Enabled = resolvedOptions.Enabled;
        FadeFrames = Enabled
            ? Math.Max(1, (int)Math.Round(sampleRate * resolvedOptions.FadeMilliseconds / 1000.0, MidpointRounding.AwayFromZero))
            : 0;
        FadeSamples = FadeFrames * channels;
    }

    public bool Enabled { get; }

    public int FadeFrames { get; }

    public int FadeSamples { get; }

    public float ApplyOutputGain(float sample, int framesRemainingInCurrentBeat)
    {
        sample = SanitizeSample(sample);

        if (!Enabled || framesRemainingInCurrentBeat > FadeFrames)
        {
            return sample;
        }

        double gain = Math.Clamp(framesRemainingInCurrentBeat / (double)FadeFrames, 0, 1);
        return (float)(sample * gain);
    }

    public float ApplyInputGain(float sample, int framesSinceBeatStart, BranchTransitionKind transitionKind)
    {
        sample = SanitizeSample(sample);

        if (!Enabled || transitionKind != BranchTransitionKind.BranchJump || framesSinceBeatStart >= FadeFrames)
        {
            return sample;
        }

        double gain = Math.Clamp(framesSinceBeatStart / (double)FadeFrames, 0, 1);
        return (float)(sample * gain);
    }

    private static float SanitizeSample(float sample)
    {
        return float.IsFinite(sample) ? sample : 0;
    }
}
