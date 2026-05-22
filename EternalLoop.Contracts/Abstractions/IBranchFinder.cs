using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using System.Collections.Generic;

namespace EternalLoop.Contracts.Abstractions;

public interface IBranchFinder
{
    IReadOnlyList<JukeboxEdge> FindBranches(IReadOnlyList<Beat> beats, BranchFindingOptions options);

    IReadOnlyList<JukeboxEdge> FindBranches(TrackAnalysis analysis, BranchFindingOptions options);
}
