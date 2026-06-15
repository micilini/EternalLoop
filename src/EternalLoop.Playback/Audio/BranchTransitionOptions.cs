namespace EternalLoop.Playback.Audio;

public sealed class BranchTransitionOptions
{
    public bool Enabled { get; init; } = true;

    public double FadeMilliseconds { get; init; } = 8;

    public double MaxFadeMilliseconds { get; init; } = 12;

    public double MinFadeMilliseconds { get; init; } = 1;

    public BranchTransitionOptions Normalize()
    {
        double min = double.IsNaN(MinFadeMilliseconds) || double.IsInfinity(MinFadeMilliseconds) || MinFadeMilliseconds <= 0
            ? 1
            : MinFadeMilliseconds;
        double max = double.IsNaN(MaxFadeMilliseconds) || double.IsInfinity(MaxFadeMilliseconds) || MaxFadeMilliseconds < min
            ? 12
            : MaxFadeMilliseconds;

        if (max < min)
        {
            min = 1;
            max = 12;
        }

        double fade = double.IsNaN(FadeMilliseconds) || double.IsInfinity(FadeMilliseconds)
            ? 8
            : FadeMilliseconds;

        return new BranchTransitionOptions
        {
            Enabled = Enabled,
            FadeMilliseconds = Math.Clamp(fade, min, max),
            MinFadeMilliseconds = min,
            MaxFadeMilliseconds = max
        };
    }
}
