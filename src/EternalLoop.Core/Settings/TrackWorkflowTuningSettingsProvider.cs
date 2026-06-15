namespace EternalLoop.Core.Settings;

public sealed class TrackWorkflowTuningSettingsProvider
{
    public TrackWorkflowTuningSettingsProvider(LoopTuningSettings settings)
    {
        Current = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public LoopTuningSettings Current { get; }
}
