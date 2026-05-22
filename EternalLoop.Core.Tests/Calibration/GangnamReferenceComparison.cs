using System.Globalization;
using System.Text;
using System.Text.Json;

namespace EternalLoop.Core.Tests.Calibration;

public sealed class GangnamReferenceComparison
{
    private const double DurationToleranceRatio = 0.08;
    private const double BeatToleranceRatio = 0.10;
    private const double EdgeCountLowerRatio = 0.60;
    private const double EdgeCountUpperRatio = 1.60;
    private const double SourceCountLowerRatio = 0.10;
    private const double SourceCountUpperRatio = 0.45;
    public const string ReferenceMismatchMessage = "The local audio and the Infinite Jukebox SVG do not appear to represent the same track version. Do not tune branch thresholds against this reference until the audio and SVG are aligned.";

    public GangnamReferenceComparisonResult Compare(
        InfiniteJukeboxSvgSummary svg,
        EternalLoopBranchSummary balanced,
        EternalLoopBranchSummary wild)
    {
        var estimatedSvgDuration = balanced.Tempo <= 0.0
            ? 0.0
            : svg.BeatTileCount * 60.0 / balanced.Tempo;
        var durationDeltaRatio = balanced.DurationSeconds <= 0.0
            ? 1.0
            : Math.Abs(balanced.DurationSeconds - estimatedSvgDuration) / balanced.DurationSeconds;
        var beatDeltaRatio = balanced.BeatCount <= 0
            ? 1.0
            : Math.Abs(balanced.BeatCount - svg.BeatTileCount) / (double)balanced.BeatCount;
        var referenceCompatible = durationDeltaRatio <= DurationToleranceRatio ||
            beatDeltaRatio <= BeatToleranceRatio;

        var failures = new List<string>();
        if (!referenceCompatible)
        {
            failures.Add(ReferenceMismatchMessage);
        }
        else
        {
            var lowerEdgeBound = svg.BranchPathCount * EdgeCountLowerRatio;
            var upperEdgeBound = svg.BranchPathCount * EdgeCountUpperRatio;
            var lowerSourceBound = balanced.BeatCount * SourceCountLowerRatio;
            var upperSourceBound = balanced.BeatCount * SourceCountUpperRatio;

            if (balanced.EdgeCount <= 0)
            {
                failures.Add("Balanced produced zero edges.");
            }

            if (wild.EdgeCount <= 0)
            {
                failures.Add("Wild produced zero edges.");
            }

            if (wild.EdgeCount < balanced.EdgeCount)
            {
                failures.Add("Wild produced fewer edges than Balanced.");
            }

            if (wild.SourceCount < balanced.SourceCount)
            {
                failures.Add("Wild produced fewer source beats than Balanced.");
            }

            if (balanced.EdgeCount < lowerEdgeBound || balanced.EdgeCount > upperEdgeBound)
            {
                failures.Add($"Balanced edge count is outside the accepted reference band [{Format(lowerEdgeBound)}, {Format(upperEdgeBound)}].");
            }

            if (balanced.SourceCount < lowerSourceBound || balanced.SourceCount > upperSourceBound)
            {
                failures.Add($"Balanced source count is outside the accepted source band [{Format(lowerSourceBound)}, {Format(upperSourceBound)}].");
            }
        }

        return new GangnamReferenceComparisonResult(
            svg,
            balanced,
            wild,
            estimatedSvgDuration,
            durationDeltaRatio,
            beatDeltaRatio,
            referenceCompatible,
            failures.Count == 0,
            failures);
    }

    public static void WriteReports(
        GangnamReferenceComparisonResult result,
        string outputDirectory,
        IReadOnlyList<GangnamCalibrationCandidate>? balancedCandidates = null,
        IReadOnlyList<GangnamCalibrationCandidate>? wildCandidates = null)
    {
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(
            Path.Combine(outputDirectory, "comparison-report.md"),
            BuildMarkdown(result, balancedCandidates, wildCandidates));
        File.WriteAllText(
            Path.Combine(outputDirectory, "comparison-report.branch-calibration.json"),
            JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string BuildMarkdown(
        GangnamReferenceComparisonResult result,
        IReadOnlyList<GangnamCalibrationCandidate>? balancedCandidates,
        IReadOnlyList<GangnamCalibrationCandidate>? wildCandidates)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Gangnam Style Branch Calibration");
        builder.AppendLine();
        builder.AppendLine($"ReferenceCompatible: {result.ReferenceCompatible}");
        builder.AppendLine($"Pass: {result.Pass}");
        builder.AppendLine($"LocalDurationSeconds: {Format(result.Balanced.DurationSeconds)}");
        builder.AppendLine($"LocalTempo: {Format(result.Balanced.Tempo)}");
        builder.AppendLine($"LocalBeatCount: {result.Balanced.BeatCount}");
        builder.AppendLine($"SvgBeatTileCount: {result.Svg.BeatTileCount}");
        builder.AppendLine($"SvgBranchPathCount: {result.Svg.BranchPathCount}");
        builder.AppendLine($"EstimatedSvgDurationFromLocalTempo: {Format(result.EstimatedSvgDuration)}");
        builder.AppendLine($"DurationDeltaRatio: {Format(result.DurationDeltaRatio)}");
        builder.AppendLine($"BeatDeltaRatio: {Format(result.BeatDeltaRatio)}");
        builder.AppendLine();
        AppendSummary(builder, result.Balanced);
        AppendSummary(builder, result.Wild);

        if (result.Failures.Count > 0)
        {
            builder.AppendLine("## Failures");
            foreach (var failure in result.Failures)
            {
                builder.AppendLine($"- {failure}");
            }

            builder.AppendLine();
        }

        AppendCandidates(builder, "Top 10 Balanced candidates", balancedCandidates);
        AppendCandidates(builder, "Top 10 Wild candidates", wildCandidates);
        return builder.ToString();
    }

    private static void AppendSummary(StringBuilder builder, EternalLoopBranchSummary summary)
    {
        builder.AppendLine($"## {summary.Preset}");
        builder.AppendLine($"UseAi: {summary.UseAi}");
        builder.AppendLine($"EdgeCount: {summary.EdgeCount}");
        builder.AppendLine($"SourceCount: {summary.SourceCount}");
        builder.AppendLine($"SourceRatio: {Format(summary.SourceRatio)}");
        builder.AppendLine($"BackwardEdgeCount: {summary.BackwardEdgeCount}");
        builder.AppendLine($"ForwardEdgeCount: {summary.ForwardEdgeCount}");
        builder.AppendLine($"LongBackwardEdgeCount: {summary.LongBackwardEdgeCount}");
        builder.AppendLine($"MetricMatchedEdgeCount: {summary.MetricMatchedEdgeCount}");
        builder.AppendLine($"CsvPath: {summary.CsvPath}");
        builder.AppendLine($"SummaryPath: {summary.SummaryPath}");
        builder.AppendLine();
    }

    private static void AppendCandidates(
        StringBuilder builder,
        string title,
        IReadOnlyList<GangnamCalibrationCandidate>? candidates)
    {
        if (candidates is null || candidates.Count == 0)
        {
            return;
        }

        builder.AppendLine($"## {title}");
        foreach (var candidate in candidates.Take(10))
        {
            builder.AppendLine($"- Score={Format(candidate.CandidateScore)} Edges={candidate.Summary.EdgeCount} Sources={candidate.Summary.SourceCount} Threshold={Format(candidate.Options.SimilarityThreshold)} Lookahead={candidate.Options.LookaheadDepth} Continuation={candidate.Options.ContinuationLookaheadDepth} Margin={Format(candidate.Options.ContinuationThresholdMargin)} MicroStart={Format(candidate.Options.MicrosegmentPenaltyStartThreshold)} MicroReject={Format(candidate.Options.MicrosegmentRejectionThreshold)} MicroStrength={Format(candidate.Options.MicrosegmentPenaltyStrength)} MaxSource={Format(candidate.Options.MaxBranchSourceRatio)}");
        }

        builder.AppendLine();
    }

    private static string Format(double value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }
}

public sealed record GangnamReferenceComparisonResult(
    InfiniteJukeboxSvgSummary Svg,
    EternalLoopBranchSummary Balanced,
    EternalLoopBranchSummary Wild,
    double EstimatedSvgDuration,
    double DurationDeltaRatio,
    double BeatDeltaRatio,
    bool ReferenceCompatible,
    bool Pass,
    IReadOnlyList<string> Failures);
