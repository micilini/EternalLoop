using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.Diagnostics;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Diagnostics;

public sealed class BranchQualityDiagnosticWriterTests
{
    private const string ExportVariable = "ETERNALLOOP_EXPORT_BRANCH_CSV";

    [Fact]
    public void WriteIfEnabled_Should_NotCreateFiles_WhenEnvironmentVariableIsDisabled()
    {
        var previous = Environment.GetEnvironmentVariable(ExportVariable);
        var directory = CreateTempDirectory();

        try
        {
            Environment.SetEnvironmentVariable(ExportVariable, null);

            var result = BranchQualityDiagnosticWriter.WriteIfEnabled(
                CreateAnalysis(),
                CreateGraph(),
                CreateOptions(),
                directory);

            result.Should().BeNull();
            Directory.GetFiles(directory).Should().BeEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable(ExportVariable, previous);
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void WriteIfEnabled_Should_CreateCsvAndSummary_WhenEnvironmentVariableIsEnabled()
    {
        var previous = Environment.GetEnvironmentVariable(ExportVariable);
        var directory = CreateTempDirectory();

        try
        {
            Environment.SetEnvironmentVariable(ExportVariable, "1");

            var result = BranchQualityDiagnosticWriter.WriteIfEnabled(
                CreateAnalysis(),
                CreateGraph(),
                CreateOptions(),
                directory);

            result.Should().NotBeNull();
            File.Exists(result!.CsvPath).Should().BeTrue();
            File.Exists(result.SummaryPath).Should().BeTrue();

            var csv = File.ReadAllText(result.CsvPath);
            var summary = File.ReadAllText(result.SummaryPath);

            csv.Should().Contain("fromBeat,toBeat,anchorBeat,landingBeat,similarity,fromStart,toStart,fromDuration,toDuration,fromConfidence,toConfidence,distanceBeats,direction,fromMetricSlot,anchorMetricSlot,landingMetricSlot,sourceAnchorMetricMatched,sourceLandingMetricMatched");
            csv.Should().Contain("0,8,7,8,0.95,0,4,0.5,0.5,1,1,8,forward,0,3,0,False,True");
            summary.Should().Contain("EternalLoop Branch Quality Diagnostics");
            summary.Should().Contain("Version: v1.2.0");
            summary.Should().Contain("FinalEdgeCount: 2");
            summary.Should().Contain("FinalSourceCount: 1");
            summary.Should().Contain("MaxBranchSourceRatio: 0.22");
            summary.Should().Contain("BackwardEdgeCount: 0");
            summary.Should().Contain("ForwardEdgeCount: 2");
            summary.Should().Contain("LongBackwardEdgeCount: 0");
            summary.Should().Contain("MetricMatchedEdgeCount: 2");
            summary.Should().Contain("MetricMatchedRatio: 1");
            summary.Should().Contain("AnchorMetricMatchedEdgeCount: 0");
            summary.Should().Contain("AnchorMetricMatchedRatio: 0");
            summary.Should().Contain("LandingMetricMatchedEdgeCount: 2");
            summary.Should().Contain("LandingMetricMatchedRatio: 1");
            summary.Should().Contain("EdgeToBeatRatio: 0.125");
            summary.Should().Contain("SourceToBeatRatio: 0.0625");
            summary.Should().Contain("PresetLikeThreshold: 0.86");
            summary.Should().NotContain("C:\\");
        }
        finally
        {
            Environment.SetEnvironmentVariable(ExportVariable, previous);
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void WriteIfEnabled_Should_ReturnNull_WhenIoFails()
    {
        var previous = Environment.GetEnvironmentVariable(ExportVariable);

        try
        {
            Environment.SetEnvironmentVariable(ExportVariable, "1");

            var result = BranchQualityDiagnosticWriter.WriteIfEnabled(
                CreateAnalysis(),
                CreateGraph(),
                CreateOptions(),
                string.Empty);

            result.Should().BeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable(ExportVariable, previous);
        }
    }

    [Fact]
    public void WriteIfEnabled_Should_WriteAnchorAndLandingMetricSlots()
    {
        var previous = Environment.GetEnvironmentVariable(ExportVariable);
        var directory = CreateTempDirectory();

        try
        {
            Environment.SetEnvironmentVariable(ExportVariable, "1");

            var result = BranchQualityDiagnosticWriter.WriteIfEnabled(
                CreateAnalysis(),
                CreateGraph(),
                CreateOptions(),
                directory);

            var csv = File.ReadAllText(result!.CsvPath);

            csv.Should().Contain("anchorBeat");
            csv.Should().Contain("landingBeat");
            csv.Should().Contain("anchorMetricSlot");
            csv.Should().Contain("landingMetricSlot");
        }
        finally
        {
            Environment.SetEnvironmentVariable(ExportVariable, previous);
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void WriteIfEnabled_Should_UseAnchorMetricMatch_WhenLandingOffsetIsNonZero()
    {
        var previous = Environment.GetEnvironmentVariable(ExportVariable);
        var directory = CreateTempDirectory();

        try
        {
            Environment.SetEnvironmentVariable(ExportVariable, "1");

            var result = BranchQualityDiagnosticWriter.WriteIfEnabled(
                CreateAnalysis(),
                CreateGraph(),
                CreateOptions(),
                directory);

            var summary = File.ReadAllText(result!.SummaryPath);

            summary.Should().Contain("AnchorMetricMatchedEdgeCount: 0");
            summary.Should().Contain("LandingMetricMatchedEdgeCount: 2");
        }
        finally
        {
            Environment.SetEnvironmentVariable(ExportVariable, previous);
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "EternalLoopDiagnosticsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static TrackAnalysis CreateAnalysis()
    {
        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = "abcdef123456",
                FilePath = "C:\\Music\\track.mp3",
                DurationSeconds = 16.0,
                SampleRate = 22_050,
                Tempo = 120.0,
                TimeSignature = 4,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion
            },
            Segments = [],
            Beats = Enumerable.Range(0, 16)
                .Select(index => new Beat
                {
                    Index = index,
                    Start = index * 0.5,
                    Duration = 0.5,
                    Confidence = 1.0,
                    Timbre = [1f, 0f],
                    Pitches = [1f, 0f],
                    Loudness = [1f, 1f, 1f],
                    BarPosition = [1f, 0f]
                })
                .ToArray(),
            Bars = [],
            Tatums = [],
            Sections = []
        };
    }

    private static JukeboxGraph CreateGraph()
    {
        return new JukeboxGraph
        {
            Nodes = Enumerable.Range(0, 16)
                .Select(index => new JukeboxNode
                {
                    BeatIndex = index,
                    Start = index * 0.5,
                    Duration = 0.5
                })
                .ToArray(),
            JumpEdges = new Dictionary<int, List<JukeboxEdge>>
            {
                [0] =
                [
                    new JukeboxEdge
                    {
                        FromBeat = 0,
                        ToBeat = 8,
                        Similarity = 0.95
                    },
                    new JukeboxEdge
                    {
                        FromBeat = 0,
                        ToBeat = 12,
                        Similarity = 0.90
                    }
                ]
            },
            SimilarityThreshold = 0.86,
            LookaheadDepth = 4
        };
    }

    private static BranchFindingOptions CreateOptions()
    {
        return new BranchFindingOptions
        {
            SimilarityThreshold = 0.86,
            LookaheadDepth = 4,
            MinJumpDistance = 20,
            MaxBranchesPerBeat = 3,
            TargetBranchSourceRatio = 0.16,
            MaxBranchSourceRatio = 0.22,
            UseAiSimilarity = true,
            UseDurationSimilarityGate = true,
            UseConfidencePenalty = true,
            MetricPositionMode = MetricPositionMode.StrongPenalty,
            UseMicrosegmentSimilarity = true,
            MicrosegmentCount = 4
        };
    }
}
