using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.BranchAnalysis.Core.Export;
using EternalLoop.Core.Runtime;
using EternalLoop.Core.Settings;
using EternalLoop.Core.Workflow;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Runtime;

public sealed class TrackRuntimePackageBuilderTests
{
    [Fact]
    public void BuildShouldCreateRuntimePackageFromAnalysisAndBranchPayload()
    {
        using TempRuntimePackageFixture fixture = TempRuntimePackageFixture.Create();
        TrackRuntimePackage package = new TrackRuntimePackageBuilder().Build(fixture.CreateRequest());

        package.Metadata.TrackId.Should().Be("track-id");
        package.Metadata.Title.Should().Be("Test Track");
        package.Metadata.Artist.Should().Be("Local");
        package.Files.AudioPath.Should().Be(fixture.AudioPath);
        package.Files.AnalysisJsonPath.Should().Be(Path.GetFullPath(fixture.AnalysisJsonPath));
        package.Files.BranchesJsonPath.Should().Be(Path.GetFullPath(fixture.BranchesJsonPath));
        package.RuntimeTrack.Beats.Should().HaveCount(4);
        package.Summary.IsPlayable.Should().BeTrue();
    }

    [Fact]
    public void BuildShouldMapBeatsToRuntimeBeatInputs()
    {
        using TempRuntimePackageFixture fixture = TempRuntimePackageFixture.Create();
        TrackRuntimePackage package = new TrackRuntimePackageBuilder().Build(fixture.CreateRequest());

        package.RuntimeTrack.Beats[0].Which.Should().Be(0);
        package.RuntimeTrack.Beats[0].Start.Should().Be(0);
        package.RuntimeTrack.Beats[0].Duration.Should().Be(0.5);
        package.RuntimeTrack.Beats[1].Prev.Should().Be(package.RuntimeTrack.Beats[0]);
        package.RuntimeTrack.Beats[0].Next.Should().Be(package.RuntimeTrack.Beats[1]);
    }

    [Fact]
    public void BuildShouldMapActiveBranchesToRuntimeTrackNeighbors()
    {
        using TempRuntimePackageFixture fixture = TempRuntimePackageFixture.Create();
        TrackRuntimePackage package = new TrackRuntimePackageBuilder().Build(fixture.CreateRequest());

        package.RuntimeTrack.ActiveBranchCount.Should().Be(2);
        package.RuntimeTrack.Beats[0].Neighbors.Should().ContainSingle(edge => edge.ToBeat == 2);
        package.RuntimeTrack.Beats[1].Neighbors.Should().ContainSingle(edge => edge.ToBeat == 3);
    }

    [Fact]
    public void BuildShouldMapCandidateBranchesToAllNeighbors()
    {
        using TempRuntimePackageFixture fixture = TempRuntimePackageFixture.Create();
        TrackRuntimePackage package = new TrackRuntimePackageBuilder().Build(fixture.CreateRequest());

        package.RuntimeTrack.CandidateBranchCount.Should().Be(2);
        package.RuntimeTrack.Beats[2].AllNeighbors.Should().ContainSingle(edge => edge.ToBeat == 0);
        package.RuntimeTrack.Beats[3].AllNeighbors.Should().ContainSingle(edge => edge.ToBeat == 1);
    }

    [Fact]
    public void BuildShouldIgnoreInvalidBranches()
    {
        using TempRuntimePackageFixture fixture = TempRuntimePackageFixture.Create();
        BranchExportPayload payload = fixture.CreatePayload(
            activeBranches:
            [
                CreateBranch(1, "active", 0, 2, 0.1),
                CreateBranch(2, "active", 0, 0, 0.1),
                CreateBranch(3, "active", null, 1, 0.1),
                CreateBranch(4, "active", 1, 2, null),
                CreateBranch(5, "active", 1, 3, 0.2, deleted: true)
            ],
            candidateBranches:
            [
                CreateBranch(6, "candidate", 2, 0, 0.2),
                CreateBranch(7, "candidate", 2, 2, 0.2)
            ]);

        TrackRuntimePackage package = new TrackRuntimePackageBuilder().Build(fixture.CreateRequest(payload));

        package.RuntimeTrack.ActiveBranchCount.Should().Be(1);
        package.RuntimeTrack.CandidateBranchCount.Should().Be(1);
        package.IgnoredActiveBranches.Should().Be(4);
        package.IgnoredCandidateBranches.Should().Be(1);
        package.Summary.IgnoredActiveBranches.Should().Be(4);
        package.Summary.IgnoredCandidateBranches.Should().Be(1);
    }

    [Fact]
    public void BuildShouldRecordTuningSnapshot()
    {
        using TempRuntimePackageFixture fixture = TempRuntimePackageFixture.Create();
        LoopTuningSettings tuning = LoopTuningSettings.Balanced();
        TrackRuntimePackage package = new TrackRuntimePackageBuilder().Build(fixture.CreateRequest(tuning: tuning));

        package.Tuning.Preset.Should().Be(LoopTuningPresetCatalog.BalancedId);
        package.Tuning.SimilarityThreshold.Should().Be(0.86);
        package.Tuning.LookaheadDepth.Should().Be(1);
        package.Tuning.MinJumpDistance.Should().Be(4);
        package.Tuning.MaxBranchesPerBeat.Should().Be(6);
        package.Tuning.BranchQuantumType.Should().Be("beats");
        package.Tuning.BranchMaxThreshold.Should().Be(80);
        package.Tuning.AnalysisMusicalQuality.Should().BeTrue();
    }

    [Fact]
    public void BuildShouldMapPlaybackRuntimeTuningToBranchDecisionOptions()
    {
        using TempRuntimePackageFixture fixture = TempRuntimePackageFixture.Create();
        LoopTuningSettings tuning = LoopTuningSettings.Balanced();
        tuning.JumpProbability = 0.42;
        tuning.JumpCooldown = 6;
        tuning.FirstPassLinearPlaybackRatio = 0.7;

        TrackRuntimePackage package = new TrackRuntimePackageBuilder().Build(fixture.CreateRequest(tuning: tuning));

        package.BranchDecisionOptions.JumpProbability.Should().Be(0.42);
        package.BranchDecisionOptions.JumpCooldownBeats.Should().Be(6);
        package.BranchDecisionOptions.FirstPassLinearPlaybackRatio.Should().Be(0.7);
        package.Tuning.JumpProbability.Should().Be(0.42);
        package.Tuning.JumpCooldown.Should().Be(6);
        package.Tuning.FirstPassLinearPlaybackRatio.Should().Be(0.7);
    }

    [Fact]
    public void BuildShouldThrowWhenAnalysisHasNoBeats()
    {
        using TempRuntimePackageFixture fixture = TempRuntimePackageFixture.Create();
        TrackAnalysis analysis = fixture.CreateAnalysis(beats: []);
        TrackRuntimePackageBuildRequest request = fixture.CreateRequest(analysis: analysis);

        var act = () => new TrackRuntimePackageBuilder().Build(request);

        act.Should().Throw<RuntimePackageBuildException>()
            .WithMessage("*no beats*");
    }

    private static BranchExportBranch CreateBranch(
        int id,
        string status,
        int? fromBeat,
        int? toBeat,
        double? distance,
        bool deleted = false)
    {
        return new BranchExportBranch
        {
            Id = id,
            Status = status,
            FromBeat = fromBeat,
            ToBeat = toBeat,
            JumpBeats = fromBeat.HasValue && toBeat.HasValue ? toBeat.Value - fromBeat.Value : null,
            Direction = fromBeat.HasValue && toBeat.HasValue && toBeat.Value < fromBeat.Value ? "backward" : "forward",
            Distance = distance,
            Deleted = deleted
        };
    }

    private sealed class TempRuntimePackageFixture : IDisposable
    {
        private TempRuntimePackageFixture(string root)
        {
            Root = root;
            AudioPath = Path.Combine(root, "track.mp3");
            AnalysisJsonPath = Path.Combine(root, "analysis", "eternalloop-analysis.json");
            BranchesJsonPath = Path.Combine(root, "branches", "eternalloop-branches.json");

            Directory.CreateDirectory(Path.GetDirectoryName(AnalysisJsonPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(BranchesJsonPath)!);
            File.WriteAllBytes(AudioPath, [1, 2, 3, 4]);
            File.WriteAllText(AnalysisJsonPath, "{}");
            File.WriteAllText(BranchesJsonPath, "{}");
        }

        public string Root { get; }

        public string AudioPath { get; }

        public string AnalysisJsonPath { get; }

        public string BranchesJsonPath { get; }

        public static TempRuntimePackageFixture Create()
        {
            return new TempRuntimePackageFixture(Directory.CreateTempSubdirectory("eternalloop-runtime-package-").FullName);
        }

        public TrackRuntimePackageBuildRequest CreateRequest(
            BranchExportPayload? payload = null,
            TrackAnalysis? analysis = null,
            LoopTuningSettings? tuning = null)
        {
            return new TrackRuntimePackageBuildRequest
            {
                Input = TrackInput.FromFilePath(AudioPath),
                Analysis = analysis ?? CreateAnalysis(),
                RunRoot = Root,
                AnalysisJsonPath = AnalysisJsonPath,
                BranchesJsonPath = BranchesJsonPath,
                BranchPayload = payload ?? CreatePayload(),
                Tuning = tuning ?? LoopTuningSettings.Balanced(),
                SettingsSchemaVersion = 5
            };
        }

        public BranchExportPayload CreatePayload(
            IReadOnlyList<BranchExportBranch>? activeBranches = null,
            IReadOnlyList<BranchExportBranch>? candidateBranches = null)
        {
            return new BranchExportPayload
            {
                SchemaVersion = "branch-export-0.1.0",
                Track = new BranchExportTrack
                {
                    Id = "track-id",
                    Title = "Test Track",
                    Artist = "Local",
                    Duration = 2
                },
                Counts = new BranchExportCounts
                {
                    Beats = 4,
                    ActiveBranches = activeBranches?.Count ?? 2,
                    CandidateBranches = candidateBranches?.Count ?? 2
                },
                ActiveBranches = activeBranches?.ToList()
                    ?? [CreateBranch(1, "active", 0, 2, 0.1), CreateBranch(2, "active", 1, 3, 0.2)],
                CandidateBranches = candidateBranches?.ToList()
                    ?? [CreateBranch(3, "candidate", 2, 0, 0.3), CreateBranch(4, "candidate", 3, 1, 0.4)]
            };
        }

        public TrackAnalysis CreateAnalysis(IReadOnlyList<Beat>? beats = null)
        {
            return new TrackAnalysis
            {
                Metadata = new TrackMetadata
                {
                    FileHash = "hash",
                    FilePath = AudioPath,
                    DurationSeconds = 2,
                    SampleRate = 44100,
                    Tempo = 120,
                    TimeSignature = 4,
                    SchemaVersion = TrackAnalysis.CurrentSchemaVersion
                },
                Beats = beats ??
                [
                    CreateBeat(0, 0),
                    CreateBeat(1, 0.5),
                    CreateBeat(2, 1.0),
                    CreateBeat(3, 1.5)
                ],
                Bars = [],
                Tatums = [],
                Segments =
                [
                    new Segment
                    {
                        Start = 0,
                        Duration = 2,
                        Confidence = 1,
                        LoudnessStart = -10,
                        LoudnessMax = -5,
                        LoudnessMaxTime = 0.1,
                        Timbre = [0],
                        Pitches = [0]
                    }
                ],
                Sections = []
            };
        }

        public void Dispose()
        {
            Directory.Delete(Root, recursive: true);
        }

        private static Beat CreateBeat(int index, double start)
        {
            return new Beat
            {
                Index = index,
                Start = start,
                Duration = 0.5,
                Confidence = 1,
                Timbre = [0],
                Pitches = [0],
                Loudness = [0],
                BarPosition = [1, 0, 0, 0]
            };
        }
    }
}
