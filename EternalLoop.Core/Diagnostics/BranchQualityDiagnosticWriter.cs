using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EternalLoop.Contracts;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;

namespace EternalLoop.Core.Diagnostics;

public static class BranchQualityDiagnosticWriter
{
    private const string ExportEnvironmentVariable = "ETERNALLOOP_EXPORT_BRANCH_CSV";
    private const string EnabledValue = "1";
    private const string ApplicationFolderName = "EternalLoop";
    private const string DiagnosticsFolderName = "Diagnostics";
    private const string BranchQualityFolderName = "BranchQuality";
    private const int HashLength = 8;

    public static BranchQualityDiagnosticResult? WriteIfEnabled(
        TrackAnalysis analysis,
        JukeboxGraph graph,
        BranchFindingOptions options,
        string? outputDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);

        if (!string.Equals(
                Environment.GetEnvironmentVariable(ExportEnvironmentVariable),
                EnabledValue,
                StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            var directory = outputDirectory ?? GetDefaultOutputDirectory();
            Directory.CreateDirectory(directory);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var hash = SanitizeHash(analysis.Metadata.FileHash);
            var csvPath = Path.Combine(directory, $"branch-quality-{timestamp}-{hash}.csv");
            var summaryPath = Path.Combine(directory, $"branch-quality-{timestamp}-{hash}-summary.txt");
            var edges = graph.JumpEdges
                .SelectMany(pair => pair.Value)
                .OrderBy(edge => edge.FromBeat)
                .ThenByDescending(edge => edge.Similarity)
                .ThenBy(edge => edge.ToBeat)
                .ToArray();

            File.WriteAllText(csvPath, BuildCsv(analysis, edges, options), Encoding.UTF8);
            File.WriteAllText(summaryPath, BuildSummary(analysis, edges, options), Encoding.UTF8);

            return new BranchQualityDiagnosticResult(csvPath, summaryPath);
        }
        catch
        {
            return null;
        }
    }

    private static string GetDefaultOutputDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(
            localAppData,
            ApplicationFolderName,
            DiagnosticsFolderName,
            BranchQualityFolderName);
    }

    private static string BuildCsv(
        TrackAnalysis analysis,
        IReadOnlyList<JukeboxEdge> edges,
        BranchFindingOptions options)
    {
        var beats = analysis.Beats.ToDictionary(beat => beat.Index);
        var timeSignature = Math.Max(1, analysis.Metadata.TimeSignature);
        var builder = new StringBuilder();
        builder.AppendLine("fromBeat,toBeat,anchorBeat,landingBeat,similarity,fromStart,toStart,fromDuration,toDuration,fromConfidence,toConfidence,distanceBeats,direction,fromMetricSlot,anchorMetricSlot,landingMetricSlot,sourceAnchorMetricMatched,sourceLandingMetricMatched");

        foreach (var edge in edges)
        {
            beats.TryGetValue(edge.FromBeat, out var fromBeat);
            beats.TryGetValue(edge.ToBeat, out var toBeat);
            var landingBeat = edge.ToBeat;
            var anchorBeat = GetAnchorBeat(landingBeat, options, analysis.Beats.Count);
            var distance = edge.ToBeat - edge.FromBeat;
            var direction = distance >= 0 ? "forward" : "backward";
            var fromMetricSlot = GetMetricSlot(edge.FromBeat, timeSignature);
            var anchorMetricSlot = GetMetricSlot(anchorBeat, timeSignature);
            var landingMetricSlot = GetMetricSlot(landingBeat, timeSignature);

            builder
                .Append(edge.FromBeat.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(edge.ToBeat.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(anchorBeat.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(landingBeat.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(Format(edge.Similarity)).Append(',')
                .Append(Format(fromBeat?.Start ?? 0.0)).Append(',')
                .Append(Format(toBeat?.Start ?? 0.0)).Append(',')
                .Append(Format(fromBeat?.Duration ?? 0.0)).Append(',')
                .Append(Format(toBeat?.Duration ?? 0.0)).Append(',')
                .Append(Format(fromBeat?.Confidence ?? 0.0)).Append(',')
                .Append(Format(toBeat?.Confidence ?? 0.0)).Append(',')
                .Append(Math.Abs(distance).ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(direction).Append(',')
                .Append(fromMetricSlot.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(anchorMetricSlot.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(landingMetricSlot.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append((fromMetricSlot == anchorMetricSlot).ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append((fromMetricSlot == landingMetricSlot).ToString(CultureInfo.InvariantCulture))
                .AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildSummary(
        TrackAnalysis analysis,
        IReadOnlyList<JukeboxEdge> edges,
        BranchFindingOptions options)
    {
        var sourceCount = edges.Select(edge => edge.FromBeat).Distinct().Count();
        var timeSignature = Math.Max(1, analysis.Metadata.TimeSignature);
        var backwardEdgeCount = edges.Count(edge => edge.ToBeat < edge.FromBeat);
        var forwardEdgeCount = edges.Count(edge => edge.ToBeat > edge.FromBeat);
        var longBackwardDistance = analysis.Beats.Count * 0.10;
        var longBackwardEdgeCount = edges.Count(edge =>
            edge.ToBeat < edge.FromBeat &&
            Math.Abs(edge.ToBeat - edge.FromBeat) >= longBackwardDistance);
        var landingMetricMatchedEdgeCount = edges.Count(edge =>
            GetMetricSlot(edge.FromBeat, timeSignature) == GetMetricSlot(edge.ToBeat, timeSignature));
        var anchorMetricMatchedEdgeCount = edges.Count(edge =>
            GetMetricSlot(edge.FromBeat, timeSignature) ==
            GetMetricSlot(GetAnchorBeat(edge.ToBeat, options, analysis.Beats.Count), timeSignature));
        var landingMetricMatchedRatio = edges.Count == 0
            ? 0.0
            : landingMetricMatchedEdgeCount / (double)edges.Count;
        var anchorMetricMatchedRatio = edges.Count == 0
            ? 0.0
            : anchorMetricMatchedEdgeCount / (double)edges.Count;
        var edgeToBeatRatio = analysis.Beats.Count == 0
            ? 0.0
            : edges.Count / (double)analysis.Beats.Count;
        var finalSourceRatio = analysis.Beats.Count == 0
            ? 0.0
            : sourceCount / (double)analysis.Beats.Count;
        var averageEdgesPerSource = sourceCount == 0
            ? 0.0
            : edges.Count / (double)sourceCount;
        var builder = new StringBuilder();

        builder.AppendLine("EternalLoop Branch Quality Diagnostics");
        builder.AppendLine($"Version: {ProductInfo.DisplayVersion}");
        builder.AppendLine($"BeatCount: {analysis.Beats.Count.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"FinalEdgeCount: {edges.Count.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"FinalSourceCount: {sourceCount.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"FinalSourceRatio: {Format(finalSourceRatio)}");
        builder.AppendLine($"AverageEdgesPerSource: {Format(averageEdgesPerSource)}");
        builder.AppendLine($"BackwardEdgeCount: {backwardEdgeCount.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"ForwardEdgeCount: {forwardEdgeCount.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"LongBackwardEdgeCount: {longBackwardEdgeCount.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"MetricMatchedEdgeCount: {landingMetricMatchedEdgeCount.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"MetricMatchedRatio: {Format(landingMetricMatchedRatio)}");
        builder.AppendLine($"AnchorMetricMatchedEdgeCount: {anchorMetricMatchedEdgeCount.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"AnchorMetricMatchedRatio: {Format(anchorMetricMatchedRatio)}");
        builder.AppendLine($"LandingMetricMatchedEdgeCount: {landingMetricMatchedEdgeCount.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"LandingMetricMatchedRatio: {Format(landingMetricMatchedRatio)}");
        builder.AppendLine($"EdgeToBeatRatio: {Format(edgeToBeatRatio)}");
        builder.AppendLine($"SourceToBeatRatio: {Format(finalSourceRatio)}");
        builder.AppendLine($"PresetLikeThreshold: {Format(options.SimilarityThreshold)}");
        builder.AppendLine($"SimilarityThreshold: {Format(options.SimilarityThreshold)}");
        builder.AppendLine($"LookaheadDepth: {options.LookaheadDepth.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"MinJumpDistance: {options.MinJumpDistance.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"MaxBranchesPerBeat: {options.MaxBranchesPerBeat.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"TargetBranchSourceRatio: {Format(options.TargetBranchSourceRatio)}");
        builder.AppendLine($"MaxBranchSourceRatio: {Format(options.MaxBranchSourceRatio)}");
        builder.AppendLine($"UseAiSimilarity: {options.UseAiSimilarity.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"UseDurationSimilarityGate: {options.UseDurationSimilarityGate.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"UseConfidencePenalty: {options.UseConfidencePenalty.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"MetricPositionMode: {options.MetricPositionMode}");
        builder.AppendLine($"UseMicrosegmentSimilarity: {options.UseMicrosegmentSimilarity.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"MicrosegmentCount: {options.MicrosegmentCount.ToString(CultureInfo.InvariantCulture)}");

        return builder.ToString();
    }

    private static int GetMetricSlot(int beatIndex, int timeSignature)
    {
        return Math.Abs(beatIndex % timeSignature);
    }

    private static int GetAnchorBeat(int landingBeat, BranchFindingOptions options, int beatCount)
    {
        if (beatCount <= 0)
        {
            return 0;
        }

        var offset = Math.Clamp(options.LandingOffsetBeats, 0, Math.Max(0, beatCount - 1));
        return Math.Clamp(landingBeat - offset, 0, beatCount - 1);
    }

    private static string SanitizeHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return "unknown0";
        }

        var clean = new string(hash.Where(char.IsLetterOrDigit).ToArray());
        if (clean.Length >= HashLength)
        {
            return clean[..HashLength];
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)))[..HashLength].ToLowerInvariant();
    }

    private static string Format(double value)
    {
        return (double.IsFinite(value) ? value : 0.0).ToString("0.######", CultureInfo.InvariantCulture);
    }
}

public sealed record BranchQualityDiagnosticResult(string CsvPath, string SummaryPath);
