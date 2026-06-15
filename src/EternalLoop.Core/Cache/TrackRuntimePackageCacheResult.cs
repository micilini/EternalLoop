using EternalLoop.Core.Runtime;
using EternalLoop.Core.Workflow;

namespace EternalLoop.Core.Cache;

public sealed record TrackRuntimePackageCacheResult(
    bool IsHit,
    TrackRuntimePackage? RuntimePackage,
    TrackAnalysisSummary? AnalysisSummary,
    TrackBranchSummary? BranchSummary)
{
    public static TrackRuntimePackageCacheResult Miss { get; } = new(false, null, null, null);

    public static TrackRuntimePackageCacheResult Hit(TrackRuntimePackage package)
    {
        return new TrackRuntimePackageCacheResult(
            true,
            package,
            new TrackAnalysisSummary(
                TimeSpan.FromSeconds(package.Metadata.DurationSeconds),
                package.Summary.RuntimeBeatCount,
                SegmentCount: 0,
                SectionCount: 0),
            new TrackBranchSummary(
                package.Summary.RuntimeBranchCount,
                package.RuntimeTrack.CandidateBranchCount,
                package.Summary.IgnoredActiveBranches,
                package.Summary.IgnoredCandidateBranches));
    }
}
