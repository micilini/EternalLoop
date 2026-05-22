using EternalLoop.Contracts.Options;
using FluentAssertions;
using Xunit.Abstractions;

namespace EternalLoop.Core.Tests.Calibration;

public sealed class GangnamStyleReferenceCalibrationTests
{
    private const string RunVariable = "ETERNALLOOP_RUN_GANGNAM_CALIBRATION";
    private const string AudioPathVariable = "ETERNALLOOP_GANGNAM_AUDIO_PATH";
    private const string SvgPathVariable = "ETERNALLOOP_GANGNAM_SVG_PATH";
    private const string OutputDirectoryVariable = "ETERNALLOOP_GANGNAM_OUTPUT_DIR";
    private const string AllowReferenceMismatchVariable = "ETERNALLOOP_GANGNAM_ALLOW_REFERENCE_MISMATCH";

    private readonly ITestOutputHelper _output;

    public GangnamStyleReferenceCalibrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "Calibration")]
    [Trait("Reference", "GangnamStyle")]
    public async Task Gangnam_reference_calibration_should_match_svg_when_reference_is_compatible()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(RunVariable), "1", StringComparison.Ordinal))
        {
            _output.WriteLine("Gangnam calibration skipped. Set ETERNALLOOP_RUN_GANGNAM_CALIBRATION=1 to run.");
            return;
        }

        var repoRoot = FindRepoRoot();
        var audioPath = ResolvePath(
            Environment.GetEnvironmentVariable(AudioPathVariable),
            Path.Combine(repoRoot, "audio.mp3"));
        var svgPath = ResolvePath(
            Environment.GetEnvironmentVariable(SvgPathVariable),
            Path.Combine(repoRoot, "infinite-jukebox-gangnam.svg"));
        var outputDirectory = ResolvePath(
            Environment.GetEnvironmentVariable(OutputDirectoryVariable),
            Path.Combine(repoRoot, "artifacts", "gangnam-calibration"));
        var allowReferenceMismatch = string.Equals(
            Environment.GetEnvironmentVariable(AllowReferenceMismatchVariable),
            "1",
            StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                Environment.GetEnvironmentVariable(AllowReferenceMismatchVariable),
                "true",
                StringComparison.OrdinalIgnoreCase);

        File.Exists(audioPath).Should().BeTrue($"Gangnam audio file is required at {audioPath}");
        File.Exists(svgPath).Should().BeTrue($"Infinite Jukebox SVG file is required at {svgPath}");
        Directory.CreateDirectory(outputDirectory);

        var svg = InfiniteJukeboxSvgSummaryParser.ParseFile(svgPath);
        var runner = new GangnamCalibrationRunner();
        var run = await runner.AnalyzeAsync(audioPath, outputDirectory);
        var comparison = new GangnamReferenceComparison();
        var result = comparison.Compare(svg, run.Balanced, run.Wild);
        IReadOnlyList<GangnamCalibrationCandidate>? balancedCandidates = null;
        IReadOnlyList<GangnamCalibrationCandidate>? wildCandidates = null;

        GangnamReferenceComparison.WriteReports(result, outputDirectory);

        if (result.ReferenceCompatible && !result.Pass)
        {
            var sweep = new GangnamCalibrationSweep(runner);
            balancedCandidates = sweep.Run(
                run.Analysis,
                TuningPresetCatalog.BalancedId,
                svg,
                null,
                Path.Combine(outputDirectory, "sweep-balanced"));
            wildCandidates = sweep.Run(
                run.Analysis,
                TuningPresetCatalog.WildId,
                svg,
                run.Balanced,
                Path.Combine(outputDirectory, "sweep-wild"));
        }

        GangnamReferenceComparison.WriteReports(result, outputDirectory, balancedCandidates, wildCandidates);

        _output.WriteLine($"Audio path: {audioPath}");
        _output.WriteLine($"SVG path: {svgPath}");
        _output.WriteLine($"Report path: {Path.Combine(outputDirectory, "comparison-report.md")}");
        _output.WriteLine($"Local duration: {run.Balanced.DurationSeconds:0.###}");
        _output.WriteLine($"Local tempo: {run.Balanced.Tempo:0.###}");
        _output.WriteLine($"Local beat count: {run.Balanced.BeatCount}");
        _output.WriteLine($"SVG tile count: {svg.BeatTileCount}");
        _output.WriteLine($"SVG path count: {svg.BranchPathCount}");
        _output.WriteLine($"Estimated SVG duration: {result.EstimatedSvgDuration:0.###}");
        _output.WriteLine($"Reference compatible: {result.ReferenceCompatible}");
        _output.WriteLine($"Balanced edges/sources: {run.Balanced.EdgeCount}/{run.Balanced.SourceCount}");
        _output.WriteLine($"Wild edges/sources: {run.Wild.EdgeCount}/{run.Wild.SourceCount}");

        if (!result.ReferenceCompatible)
        {
            if (allowReferenceMismatch)
            {
                return;
            }

            throw new InvalidOperationException(GangnamReferenceComparison.ReferenceMismatchMessage);
        }

        if (result.Pass)
        {
            return;
        }

        if (balancedCandidates is { Count: > 0 } || wildCandidates is { Count: > 0 })
        {
            WriteSuggestedPresetValues(outputDirectory, balancedCandidates, wildCandidates);
            throw new InvalidOperationException("Gangnam reference is compatible, but current presets are outside the accepted bands. See suggested-preset-values.md.");
        }

        result.Failures.Should().BeEmpty();
    }

    private static void WriteSuggestedPresetValues(
        string outputDirectory,
        IReadOnlyList<GangnamCalibrationCandidate>? balancedCandidates,
        IReadOnlyList<GangnamCalibrationCandidate>? wildCandidates)
    {
        using var writer = new StreamWriter(Path.Combine(outputDirectory, "suggested-preset-values.md"));
        writer.WriteLine("# Suggested Gangnam Preset Values");
        WriteCandidate(writer, "Balanced", balancedCandidates?.FirstOrDefault());
        WriteCandidate(writer, "Wild", wildCandidates?.FirstOrDefault());
    }

    private static void WriteCandidate(
        TextWriter writer,
        string title,
        GangnamCalibrationCandidate? candidate)
    {
        if (candidate is null)
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine($"## {title}");
        writer.WriteLine($"SimilarityThreshold = {candidate.Options.SimilarityThreshold:0.###}");
        writer.WriteLine($"LookaheadDepth = {candidate.Options.LookaheadDepth}");
        writer.WriteLine($"ContinuationLookaheadDepth = {candidate.Options.ContinuationLookaheadDepth}");
        writer.WriteLine($"ContinuationThresholdMargin = {candidate.Options.ContinuationThresholdMargin:0.###}");
        writer.WriteLine($"MicrosegmentPenaltyStartThreshold = {candidate.Options.MicrosegmentPenaltyStartThreshold:0.###}");
        writer.WriteLine($"MicrosegmentRejectionThreshold = {candidate.Options.MicrosegmentRejectionThreshold:0.###}");
        writer.WriteLine($"MicrosegmentPenaltyStrength = {candidate.Options.MicrosegmentPenaltyStrength:0.###}");
        writer.WriteLine($"MaxBranchSourceRatio = {candidate.Options.MaxBranchSourceRatio:0.###}");
        writer.WriteLine($"EdgeCount = {candidate.Summary.EdgeCount}");
        writer.WriteLine($"SourceCount = {candidate.Summary.SourceCount}");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "EternalLoop.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate EternalLoop.slnx from the test output directory.");
    }

    private static string ResolvePath(string? configuredPath, string defaultPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath) ? defaultPath : configuredPath;
        return Path.GetFullPath(path);
    }
}
