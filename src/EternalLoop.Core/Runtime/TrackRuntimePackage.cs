using EternalLoop.Playback.Models;
using EternalLoop.Playback.Runtime;
using EternalLoop.Core.Workflow;

namespace EternalLoop.Core.Runtime;

public sealed record TrackRuntimePackage(
    TrackRuntimeMetadata Metadata,
    TrackRuntimeFileSet Files,
    TrackRuntimeTuningSnapshot Tuning,
    RuntimeTrack RuntimeTrack,
    BranchDecisionOptions BranchDecisionOptions,
    TrackRuntimePreparationSummary Summary,
    int IgnoredActiveBranches,
    int IgnoredCandidateBranches);
