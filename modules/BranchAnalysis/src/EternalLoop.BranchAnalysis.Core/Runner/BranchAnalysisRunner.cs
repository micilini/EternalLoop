using System.Text.Json.Nodes;
using EternalLoop.BranchAnalysis.Core.Branching;
using EternalLoop.BranchAnalysis.Core.Export;
using EternalLoop.BranchAnalysis.Core.IO;
using EternalLoop.BranchAnalysis.Core.Models;
using EternalLoop.BranchAnalysis.Core.Preprocessing;
using EternalLoop.BranchAnalysis.Core.Validation;

namespace EternalLoop.BranchAnalysis.Core.Runner;

public static class BranchAnalysisRunner
{
    private const string CliTitle = "EternalLoop Branch Analysis CLI";
    private const string NoAnalysisFilesMessagePrefix = "No eternalloop-analysis.json files were found in:";
    private const string DiscoveryFailureMessagePrefix = "Failed to discover analysis files:";
    private const string ItemFailureMessagePrefix = "Failed to analyze branches for";
    private const string FinalSummaryTitle = "Branch analysis finished";

    public static int Run(BranchAnalysisOptions options, TextWriter output, TextWriter error)
    {
        IReadOnlyList<AnalysisDiscoveryResult> analyses;

        try
        {
            analyses = AnalysisDiscovery.Discover(options.AnalysisRoot);
        }
        catch (AnalysisRootNotFoundException exception)
        {
            error.WriteLine(exception.Message);
            return BranchAnalysisRunnerExitCodes.AnalysisRootNotFound;
        }
        catch (Exception exception)
        {
            error.WriteLine($"{DiscoveryFailureMessagePrefix} {exception.Message}");
            return BranchAnalysisRunnerExitCodes.BranchAnalysisFailed;
        }

        if (analyses.Count == 0)
        {
            error.WriteLine($"{NoAnalysisFilesMessagePrefix} {options.AnalysisRoot}");
            return BranchAnalysisRunnerExitCodes.NoAnalysisFilesFound;
        }

        if (!options.Quiet)
        {
            WriteDiscoverySummary(options, analyses, output);
        }

        int succeeded = 0;
        int failed = 0;
        int firstFailureExitCode = BranchAnalysisRunnerExitCodes.Success;

        foreach (AnalysisDiscoveryResult analysis in analyses)
        {
            try
            {
                BranchAnalysisItemResult result = ProcessAnalysis(analysis, options);
                succeeded++;

                if (!options.Quiet)
                {
                    WriteAnalysisSuccess(result, output);
                }
            }
            catch (Exception exception)
            {
                failed++;

                if (firstFailureExitCode == BranchAnalysisRunnerExitCodes.Success)
                {
                    firstFailureExitCode = GetExitCodeForException(exception);
                }

                error.WriteLine($"{ItemFailureMessagePrefix} {analysis.Name}: {exception.Message}");
            }
        }

        if (!options.Quiet)
        {
            WriteFinalSummary(succeeded, failed, output);
        }

        return failed > 0 ? firstFailureExitCode : BranchAnalysisRunnerExitCodes.Success;
    }

    public static BranchAnalysisItemResult ProcessAnalysis(
        AnalysisDiscoveryResult analysis,
        BranchAnalysisOptions options)
    {
        JsonNode root = BranchAnalysisJsonReader.Read(analysis.AnalysisPath);
        TrackAnalysisDocument track = AnalysisContractValidator.ReadValidated(root);
        TrackPreprocessor.Preprocess(track);

        NearestNeighborOptions nearestNeighborOptions = NearestNeighborOptions.FromBranchAnalysisOptions(options);
        BranchGraphData branchGraphData = NearestNeighborCalculator.CreateBranchGraphData(
            track,
            options.QuantumType,
            nearestNeighborOptions);

        NearestNeighborCalculator.DynamicCalculateNearestNeighbors(track, branchGraphData, options.QuantumType);
        PostProcessNearestNeighbors.PostProcess(track, branchGraphData, options.QuantumType);

        BranchExportPayload payload = BranchExportPayloadBuilder.Build(track, branchGraphData, options.QuantumType);
        BranchAnalysisWriteResult writeResult = BranchAnalysisWriter.Write(
            options.OutputRoot,
            analysis.Name,
            payload,
            new BranchAnalysisWriteOptions
            {
                Force = options.Force,
                Pretty = options.Pretty
            });

        return new BranchAnalysisItemResult
        {
            Name = analysis.Name,
            TrackId = track.Info.Id,
            Beats = track.Analysis.Beats.Count,
            Segments = track.Analysis.Segments.Count,
            ActiveBranches = payload.Counts.ActiveBranches,
            CandidateBranches = payload.Counts.CandidateBranches,
            OutputPath = writeResult.OutputPath
        };
    }

    public static int GetExitCodeForException(Exception exception)
    {
        return exception switch
        {
            AnalysisRootNotFoundException => BranchAnalysisRunnerExitCodes.AnalysisRootNotFound,
            AnalysisContractValidationException => BranchAnalysisRunnerExitCodes.ValidationFailed,
            BranchOutputAlreadyExistsException => BranchAnalysisRunnerExitCodes.ExportFailed,
            BranchAnalysisJsonReadException => BranchAnalysisRunnerExitCodes.BranchAnalysisFailed,
            BranchAnalysisJsonParseException => BranchAnalysisRunnerExitCodes.BranchAnalysisFailed,
            _ => BranchAnalysisRunnerExitCodes.BranchAnalysisFailed
        };
    }

    private static void WriteDiscoverySummary(
        BranchAnalysisOptions options,
        IReadOnlyList<AnalysisDiscoveryResult> analyses,
        TextWriter output)
    {
        output.WriteLine(CliTitle);
        output.WriteLine($"Analysis root: {options.AnalysisRoot}");
        output.WriteLine($"Output root: {options.OutputRoot}");
        output.WriteLine($"Quantum type: {options.QuantumType}");
        output.WriteLine($"Similarity threshold: {options.SimilarityThreshold:0.00}");
        output.WriteLine($"Lookahead depth: {options.LookaheadDepth}");
        output.WriteLine($"Min jump distance: {options.MinJumpDistance}");
        output.WriteLine($"Max branches: {options.MaxBranches}");
        output.WriteLine($"Max threshold: {options.MaxThreshold}");
        output.WriteLine($"Found: {analyses.Count} analysis file{(analyses.Count == 1 ? string.Empty : "s")}");
    }

    private static void WriteAnalysisSuccess(BranchAnalysisItemResult result, TextWriter output)
    {
        output.WriteLine();
        output.WriteLine($"Analyzing branches: {result.Name}");
        output.WriteLine($"TrackId: {result.TrackId}");
        output.WriteLine($"Beats: {result.Beats}");
        output.WriteLine($"Segments: {result.Segments}");
        output.WriteLine($"Candidate branches: {result.CandidateBranches}");
        output.WriteLine($"Active branches: {result.ActiveBranches}");
        output.WriteLine($"Output: {result.OutputPath}");
    }

    private static void WriteFinalSummary(int succeeded, int failed, TextWriter output)
    {
        output.WriteLine();
        output.WriteLine(FinalSummaryTitle);
        output.WriteLine($"Succeeded: {succeeded}");
        output.WriteLine($"Failed: {failed}");
    }
}


