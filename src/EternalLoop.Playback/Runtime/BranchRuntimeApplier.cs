using EternalLoop.Playback.Models;

namespace EternalLoop.Playback.Runtime;

public sealed class BranchRuntimeApplier
{
    public BranchRuntimeApplyResult Apply(
        RuntimeTrack track,
        IReadOnlyList<RuntimeBranchInput> activeBranches,
        IReadOnlyList<RuntimeBranchInput> candidateBranches)
    {
        ArgumentNullException.ThrowIfNull(track);
        ArgumentNullException.ThrowIfNull(activeBranches);
        ArgumentNullException.ThrowIfNull(candidateBranches);

        int appliedActive = 0;
        int appliedCandidate = 0;
        int ignoredActive = 0;
        int ignoredCandidate = 0;

        foreach (RuntimeBranchInput branch in activeBranches)
        {
            if (TryCreateEdge(track, branch, "active", out RuntimeBranchEdge? edge) && edge is not null)
            {
                edge.SourceBeat.Neighbors.Add(edge);
                appliedActive++;
            }
            else
            {
                ignoredActive++;
            }
        }

        foreach (RuntimeBranchInput branch in candidateBranches)
        {
            if (TryCreateEdge(track, branch, "candidate", out RuntimeBranchEdge? edge) && edge is not null)
            {
                edge.SourceBeat.AllNeighbors.Add(edge);
                appliedCandidate++;
            }
            else
            {
                ignoredCandidate++;
            }
        }

        foreach (RuntimeBeat beat in track.Beats)
        {
            SortEdges(beat.Neighbors);
            SortEdges(beat.AllNeighbors);
        }

        track.ActiveBranchCount = appliedActive;
        track.CandidateBranchCount = appliedCandidate;

        return new BranchRuntimeApplyResult(appliedActive, appliedCandidate, ignoredActive, ignoredCandidate);
    }

    private static bool TryCreateEdge(
        RuntimeTrack track,
        RuntimeBranchInput branch,
        string fallbackStatus,
        out RuntimeBranchEdge? edge)
    {
        edge = null;

        if (branch.Deleted
            || branch.FromBeat < 0
            || branch.ToBeat < 0
            || branch.FromBeat >= track.Beats.Count
            || branch.ToBeat >= track.Beats.Count
            || branch.FromBeat == branch.ToBeat
            || !double.IsFinite(branch.Distance))
        {
            return false;
        }

        RuntimeBeat sourceBeat = track.Beats[branch.FromBeat];
        RuntimeBeat destinationBeat = track.Beats[branch.ToBeat];

        edge = new RuntimeBranchEdge
        {
            Id = branch.Id,
            Status = string.IsNullOrWhiteSpace(branch.Status) ? fallbackStatus : branch.Status,
            FromBeat = branch.FromBeat,
            ToBeat = branch.ToBeat,
            JumpBeats = branch.JumpBeats,
            Direction = branch.Direction,
            Distance = branch.Distance,
            Deleted = branch.Deleted,
            SourceBeat = sourceBeat,
            DestinationBeat = destinationBeat
        };

        return true;
    }

    private static void SortEdges(List<RuntimeBranchEdge> edges)
    {
        edges.Sort((left, right) =>
        {
            int distanceComparison = left.Distance.CompareTo(right.Distance);

            if (distanceComparison != 0)
            {
                return distanceComparison;
            }

            int destinationComparison = left.ToBeat.CompareTo(right.ToBeat);

            return destinationComparison != 0
                ? destinationComparison
                : left.Id.CompareTo(right.Id);
        });
    }
}
