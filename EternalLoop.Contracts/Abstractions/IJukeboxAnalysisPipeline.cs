using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EternalLoop.Contracts.Abstractions;

public interface IJukeboxAnalysisPipeline
{
    Task<JukeboxAnalysisResult> AnalyzeAsync(
        string filePath,
        IAnalysisProgressReporter progressReporter,
        CancellationToken cancellationToken,
        bool forceReanalysis = false,
        BranchFindingOptions? branchOptions = null);

    JukeboxGraph BuildGraph(
        IReadOnlyList<Beat> beats,
        BranchFindingOptions options);

    JukeboxGraph BuildGraph(
        TrackAnalysis analysis,
        BranchFindingOptions options);
}
