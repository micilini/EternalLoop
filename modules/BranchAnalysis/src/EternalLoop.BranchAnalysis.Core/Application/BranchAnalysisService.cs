using EternalLoop.BranchAnalysis.Core.IO;
using EternalLoop.BranchAnalysis.Core.Runner;

namespace EternalLoop.BranchAnalysis.Core.Application;

public sealed class BranchAnalysisService : IBranchAnalysisService
{
    public Task<BranchAnalysisResult> AnalyzeAsync(
        BranchAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        BranchAnalysisOptions options = CreateEffectiveOptions(request);

        BranchAnalysisItemResult itemResult = BranchAnalysisRunner.ProcessAnalysis(
            new AnalysisDiscoveryResult
            {
                Name = request.AnalysisName,
                DirectoryPath = request.AnalysisDirectory,
                AnalysisPath = request.AnalysisPath
            },
            options);

        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new BranchAnalysisResult(itemResult));
    }

    private static BranchAnalysisOptions CreateEffectiveOptions(BranchAnalysisRequest request)
    {
        BranchAnalysisOptions source = request.Options;

        return new BranchAnalysisOptions
        {
            AnalysisRoot = source.AnalysisRoot,
            OutputRoot = request.OutputRoot,
            QuantumType = source.QuantumType,
            SimilarityThreshold = source.SimilarityThreshold,
            LookaheadDepth = source.LookaheadDepth,
            MinJumpDistance = source.MinJumpDistance,
            MaxBranches = source.MaxBranches,
            MaxThreshold = source.MaxThreshold,
            StructuralPolicy = source.StructuralPolicy,
            AntiLocalLoopPolicy = source.AntiLocalLoopPolicy,
            ShortBranchPolicy = source.ShortBranchPolicy,
            VeryShortBars = source.VeryShortBars,
            ShortBars = source.ShortBars,
            PhraseBars = source.PhraseBars,
            LocalWindowBars = source.LocalWindowBars,
            MaxShortLocalBranchesPerCluster = source.MaxShortLocalBranchesPerCluster,
            Force = source.Force,
            Pretty = source.Pretty,
            Quiet = true
        };
    }
}
