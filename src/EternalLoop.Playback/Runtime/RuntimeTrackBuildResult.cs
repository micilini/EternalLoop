using EternalLoop.Playback.Models;

namespace EternalLoop.Playback.Runtime;

public sealed record RuntimeTrackBuildResult(
    RuntimeTrack Track,
    int IgnoredActiveBranches,
    int IgnoredCandidateBranches);
