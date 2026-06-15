using EternalLoop.Playback.Models;

namespace EternalLoop.Playback.Runtime;

public sealed class RuntimeBranchOrder
{
    private static readonly RuntimeBranchEdge[] EmptyCandidates = [];
    private readonly Dictionary<RuntimeBeat, RuntimeBranchEdge[]> _branchesByBeat;

    private RuntimeBranchOrder(Dictionary<RuntimeBeat, RuntimeBranchEdge[]> branchesByBeat)
    {
        _branchesByBeat = branchesByBeat;
    }

    public static RuntimeBranchOrder FromLinearBeatIndex(RuntimeLinearBeatIndex linearBeatIndex)
    {
        ArgumentNullException.ThrowIfNull(linearBeatIndex);

        Dictionary<RuntimeBeat, RuntimeBranchEdge[]> branchesByBeat = new(ReferenceEqualityComparer.Instance);

        for (int beatIndex = 0; beatIndex < linearBeatIndex.Beats.Count; beatIndex++)
        {
            RuntimeBeat beat = linearBeatIndex.Beats[beatIndex];

            if (beat.Neighbors.Count == 0)
            {
                continue;
            }

            RuntimeBranchEdge[] branches = new RuntimeBranchEdge[beat.Neighbors.Count];

            for (int branchIndex = 0; branchIndex < beat.Neighbors.Count; branchIndex++)
            {
                branches[branchIndex] = beat.Neighbors[branchIndex];
            }

            branchesByBeat[beat] = branches;
        }

        return new RuntimeBranchOrder(branchesByBeat);
    }

    public IReadOnlyList<RuntimeBranchEdge> GetCandidates(RuntimeBeat seedBeat)
    {
        ArgumentNullException.ThrowIfNull(seedBeat);
        return _branchesByBeat.TryGetValue(seedBeat, out RuntimeBranchEdge[]? branches)
            ? branches
            : EmptyCandidates;
    }

    public void MoveToEnd(RuntimeBeat seedBeat, RuntimeBranchEdge selectedBranch)
    {
        ArgumentNullException.ThrowIfNull(seedBeat);
        ArgumentNullException.ThrowIfNull(selectedBranch);

        if (!_branchesByBeat.TryGetValue(seedBeat, out RuntimeBranchEdge[]? branches) || branches.Length <= 1)
        {
            return;
        }

        int selectedIndex = -1;

        for (int index = 0; index < branches.Length; index++)
        {
            if (ReferenceEquals(branches[index], selectedBranch))
            {
                selectedIndex = index;
                break;
            }
        }

        if (selectedIndex < 0 || selectedIndex == branches.Length - 1)
        {
            return;
        }

        RuntimeBranchEdge selected = branches[selectedIndex];

        for (int index = selectedIndex; index < branches.Length - 1; index++)
        {
            branches[index] = branches[index + 1];
        }

        branches[^1] = selected;
    }
}
