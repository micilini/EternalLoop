using System.Text.Json;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Ai.Advisor;

public sealed class AdvisorGoldenMasterFixture
{
    public required string TrackId { get; init; }

    public required float[] Spectrogram { get; init; }

    public required int SpectrogramFrames { get; init; }

    public required int MelBins { get; init; }

    public required float[] ExpectedBeatLogits { get; init; }

    public required float[] ExpectedDownbeatLogits { get; init; }

    public required double DurationSeconds { get; init; }

    public required double FrameRate { get; init; }

    public required JsonDocument AggregateContract { get; init; }

    public required JsonDocument PostprocessContract { get; init; }

    public required JsonDocument ExpectedPostprocessBeats { get; init; }

    public required JsonDocument ExpectedPostprocessDownbeats { get; init; }
}
