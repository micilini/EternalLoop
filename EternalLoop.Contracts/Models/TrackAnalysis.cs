using System.Collections.Generic;
using EternalLoop.Contracts;

namespace EternalLoop.Contracts.Models;

public sealed class TrackAnalysis
{
    public const string CurrentSchemaVersion = ProductInfo.Version;

    public required TrackMetadata Metadata { get; init; }

    public required IReadOnlyList<Segment> Segments { get; init; }

    public required IReadOnlyList<Beat> Beats { get; init; }

    public required IReadOnlyList<Bar> Bars { get; init; }

    public required IReadOnlyList<Tatum> Tatums { get; init; }

    public required IReadOnlyList<Section> Sections { get; init; }

    public IReadOnlyList<BeatMicroFingerprint> MicroFingerprints { get; init; } = [];

    public AiAnalysisData? Ai { get; init; }
}
