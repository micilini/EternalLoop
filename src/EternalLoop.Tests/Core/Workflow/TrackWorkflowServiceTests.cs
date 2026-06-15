using EternalLoop.AnalysisEngine.Core.Application;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Progress;
using EternalLoop.BranchAnalysis.Core.Application;
using EternalLoop.BranchAnalysis.Core.Export;
using EternalLoop.BranchAnalysis.Core.Runner;
using EternalLoop.Core.Cache;
using EternalLoop.Core.Settings;
using EternalLoop.Core.Workflow;
using FluentAssertions;
using System.Text.Json;
using ApplicationBranchAnalysisResult = EternalLoop.BranchAnalysis.Core.Application.BranchAnalysisResult;

namespace EternalLoop.Tests.Core.Workflow;

public sealed class TrackWorkflowServiceTests
{
    [Fact]
    public async Task RunAsyncShouldReturnFailedWhenInputFileDoesNotExist()
    {
        var service = new TrackWorkflowService(
            new FakeAnalysisEngineService(CreateAnalysis()),
            new FakeBranchAnalysisService());

        var input = TrackInput.FromFilePath(Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"),
            "missing.mp3"));

        var result = await service.RunAsync(new TrackWorkflowRequest(input));

        result.Status.Should().Be(TrackWorkflowStatus.Failed);
        result.Error!.Code.Should().Be("input_not_found");
    }

    [Fact]
    public async Task RunAsyncShouldReturnFailedWhenInputExtensionIsUnsupported()
    {
        string tempRoot = Directory.CreateTempSubdirectory("eternalloop-core-workflow-").FullName;
        string textPath = Path.Combine(tempRoot, "track.txt");
        await File.WriteAllTextAsync(textPath, "not audio");
        var service = new TrackWorkflowService(
            new FakeAnalysisEngineService(CreateAnalysis(textPath)),
            new FakeBranchAnalysisService());

        var result = await service.RunAsync(new TrackWorkflowRequest(TrackInput.FromFilePath(textPath)));

        result.Status.Should().Be(TrackWorkflowStatus.Failed);
        result.Error!.Code.Should().Be("unsupported_audio_format");
    }

    [Fact]
    public async Task RunAsyncShouldReturnFailedWhenInputFileIsEmpty()
    {
        string tempRoot = Directory.CreateTempSubdirectory("eternalloop-core-workflow-").FullName;
        string audioPath = Path.Combine(tempRoot, "empty.mp3");
        await File.WriteAllBytesAsync(audioPath, []);
        var service = new TrackWorkflowService(
            new FakeAnalysisEngineService(CreateAnalysis(audioPath)),
            new FakeBranchAnalysisService());

        var result = await service.RunAsync(new TrackWorkflowRequest(TrackInput.FromFilePath(audioPath)));

        result.Status.Should().Be(TrackWorkflowStatus.Failed);
        result.Error!.Code.Should().Be("input_empty_file");
    }

    [Fact]
    public async Task RunAsyncShouldRunAnalysisExportBranchesAndReturnCompletedResult()
    {
        string tempRoot = Directory.CreateTempSubdirectory("eternalloop-core-workflow-").FullName;
        string audioPath = Path.Combine(tempRoot, "track.mp3");
        await File.WriteAllBytesAsync(audioPath, [1, 2, 3, 4]);

        var analysisService = new FakeAnalysisEngineService(CreateAnalysis(audioPath));
        var branchService = new FakeBranchAnalysisService();
        var progressReporter = new CapturingTrackWorkflowProgressReporter();

        var service = new TrackWorkflowService(
            analysisService,
            branchService,
            new TrackWorkflowServiceOptions
            {
                WorkspaceRoot = Path.Combine(tempRoot, "workspace")
            });

        var input = TrackInput.FromFilePath(audioPath);
        var request = new TrackWorkflowRequest(input, correlationId: "test-run");

        TrackWorkflowResult result = await service.RunAsync(request, progressReporter);

        result.Status.Should().Be(TrackWorkflowStatus.Completed);
        result.IsSuccess.Should().BeTrue();
        result.AnalysisSummary.Should().NotBeNull();
        result.AnalysisSummary!.BeatCount.Should().Be(2);
        result.BranchSummary.Should().NotBeNull();
        result.BranchSummary!.ActiveBranchCount.Should().Be(3);
        result.RuntimePackage.Should().NotBeNull();
        result.RuntimeSummary.Should().NotBeNull();
        result.RuntimeSummary!.RuntimeBeatCount.Should().Be(2);
        result.RuntimeSummary!.RuntimeBranchCount.Should().Be(3);
        result.RuntimeSummary!.IsPlayable.Should().BeTrue();
        result.RuntimePackage!.RuntimeTrack.ActiveBranchCount.Should().Be(3);
        result.RuntimePackage.Files.AudioPath.Should().Be(audioPath);
        File.Exists(result.RuntimePackage.Files.AnalysisJsonPath).Should().BeTrue();
        File.Exists(result.RuntimePackage.Files.BranchesJsonPath).Should().BeTrue();

        analysisService.Requests.Should().HaveCount(1);
        branchService.Requests.Should().HaveCount(1);
        analysisService.Requests[0].Options.Artist.Should().Be("Local");
        analysisService.Requests[0].Options.MusicalQuality.AcousticSegmentation.Should().BeTrue();
        analysisService.Requests[0].Options.MusicalQuality.BeatMicroSnap.Should().BeTrue();
        branchService.Requests[0].Options.QuantumType.Should().Be("beats");
        branchService.Requests[0].Options.MaxBranches.Should().Be(4);
        branchService.Requests[0].Options.MaxThreshold.Should().Be(80);
        branchService.Requests[0].Options.Force.Should().BeTrue();
        branchService.Requests[0].Options.Pretty.Should().BeTrue();
        branchService.Requests[0].Options.Quiet.Should().BeTrue();
        File.Exists(branchService.Requests[0].AnalysisPath).Should().BeTrue();
        progressReporter.Progress.Should().Contain(progress => progress.Status == TrackWorkflowStatus.Completed);
    }

    [Fact]
    public async Task RunAsyncShouldUseProvidedTuningForAnalysisAndBranchOptions()
    {
        string tempRoot = Directory.CreateTempSubdirectory("eternalloop-core-workflow-").FullName;
        string audioPath = Path.Combine(tempRoot, "track.mp3");
        await File.WriteAllBytesAsync(audioPath, [1, 2, 3, 4]);

        var tuning = LoopTuningSettings.Balanced();
        tuning.SimilarityThreshold = 0.78;
        tuning.LookaheadDepth = 2;
        tuning.MinJumpDistance = 16;
        tuning.MaxBranchesPerBeat = 6;
        tuning.AnalysisMusicalQuality = false;
        var analysisService = new FakeAnalysisEngineService(CreateAnalysis(audioPath));
        var branchService = new FakeBranchAnalysisService();
        var service = new TrackWorkflowService(
            analysisService,
            branchService,
            new TrackWorkflowServiceOptions
            {
                WorkspaceRoot = Path.Combine(tempRoot, "workspace"),
                Tuning = tuning
            });

        TrackWorkflowResult result = await service.RunAsync(new TrackWorkflowRequest(TrackInput.FromFilePath(audioPath)));

        analysisService.Requests[0].Options.MusicalQuality.AcousticSegmentation.Should().BeFalse();
        branchService.Requests[0].Options.MaxBranches.Should().Be(6);
        branchService.Requests[0].Options.MaxThreshold.Should().Be(95);
        branchService.Requests[0].Options.SimilarityThreshold.Should().Be(0.78);
        branchService.Requests[0].Options.LookaheadDepth.Should().Be(2);
        branchService.Requests[0].Options.MinJumpDistance.Should().Be(16);
        result.RuntimePackage.Should().NotBeNull();
        result.RuntimePackage!.Tuning.SimilarityThreshold.Should().Be(0.78);
        result.RuntimePackage.Tuning.LookaheadDepth.Should().Be(2);
        result.RuntimePackage.Tuning.MinJumpDistance.Should().Be(16);
        result.RuntimePackage.Tuning.MaxBranchesPerBeat.Should().Be(6);
        result.RuntimePackage.Tuning.BranchMaxThreshold.Should().Be(95);
        result.RuntimePackage.BranchDecisionOptions.JumpProbability.Should().Be(0.22);
        result.RuntimePackage.BranchDecisionOptions.JumpCooldownBeats.Should().Be(12);
        result.RuntimePackage.BranchDecisionOptions.FirstPassLinearPlaybackRatio.Should().Be(0.78);
    }

    [Fact]
    public async Task RunAsyncShouldSaveCacheAndReuseItWithoutCallingEngines()
    {
        string tempRoot = Directory.CreateTempSubdirectory("eternalloop-core-cache-workflow-").FullName;
        string audioPath = Path.Combine(tempRoot, "track.mp3");
        await File.WriteAllBytesAsync(audioPath, [1, 2, 3, 4]);
        var pathProvider = new AppPathProvider(Path.Combine(tempRoot, "appdata"));
        var identityService = new TrackFileIdentityService();
        var cacheService = new TrackRuntimePackageCacheService(pathProvider);

        var firstAnalysis = new FakeAnalysisEngineService(CreateAnalysis(audioPath));
        var firstBranches = new FakeBranchAnalysisService();
        var firstService = new TrackWorkflowService(
            firstAnalysis,
            firstBranches,
            new TrackWorkflowServiceOptions
            {
                WorkspaceRoot = pathProvider.WorkflowCacheDirectory,
                FileIdentityService = identityService,
                RuntimePackageCacheService = cacheService
            });

        TrackInput input = TrackInput.FromFilePath(audioPath);
        TrackWorkflowResult first = await firstService.RunAsync(new TrackWorkflowRequest(input, correlationId: "fresh"));

        first.CacheHit.Should().BeFalse();
        firstAnalysis.Requests.Should().HaveCount(1);
        firstBranches.Requests.Should().HaveCount(1);
        File.Exists(pathProvider.RuntimeCacheIndexFilePath).Should().BeTrue();

        var secondAnalysis = new FakeAnalysisEngineService(CreateAnalysis(audioPath));
        var secondBranches = new FakeBranchAnalysisService();
        var secondService = new TrackWorkflowService(
            secondAnalysis,
            secondBranches,
            new TrackWorkflowServiceOptions
            {
                WorkspaceRoot = pathProvider.WorkflowCacheDirectory,
                FileIdentityService = identityService,
                RuntimePackageCacheService = cacheService
            });

        TrackWorkflowResult second = await secondService.RunAsync(new TrackWorkflowRequest(input, correlationId: "cached"));

        second.CacheHit.Should().BeTrue();
        second.AnalysisSource.Should().Be("Cached analysis");
        second.RuntimePackage.Should().NotBeNull();
        second.RuntimeSummary!.IsPlayable.Should().BeTrue();
        secondAnalysis.Requests.Should().BeEmpty();
        secondBranches.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsyncShouldRebuildRuntimeOnlyTuningWithoutCallingEngines()
    {
        string tempRoot = Directory.CreateTempSubdirectory("eternalloop-core-cache-runtime-only-").FullName;
        string audioPath = Path.Combine(tempRoot, "track.mp3");
        await File.WriteAllBytesAsync(audioPath, [1, 2, 3, 4]);
        var pathProvider = new AppPathProvider(Path.Combine(tempRoot, "appdata"));
        var identityService = new TrackFileIdentityService();
        var cacheService = new TrackRuntimePackageCacheService(pathProvider);
        LoopTuningSettings originalTuning = LoopTuningSettings.Balanced();

        var firstService = new TrackWorkflowService(
            new FakeAnalysisEngineService(CreateAnalysis(audioPath)),
            new FakeBranchAnalysisService(),
            new TrackWorkflowServiceOptions
            {
                WorkspaceRoot = pathProvider.WorkflowCacheDirectory,
                FileIdentityService = identityService,
                RuntimePackageCacheService = cacheService,
                Tuning = originalTuning
            });

        TrackInput input = TrackInput.FromFilePath(audioPath);
        await firstService.RunAsync(new TrackWorkflowRequest(input, correlationId: "fresh"));

        LoopTuningSettings runtimeOnlyTuning = LoopTuningSettings.Balanced();
        runtimeOnlyTuning.JumpProbability = 0.91;
        runtimeOnlyTuning.JumpCooldown = 3;
        runtimeOnlyTuning.FirstPassLinearPlaybackRatio = 0.2;
        var analysis = new FakeAnalysisEngineService(CreateAnalysis(audioPath));
        var branches = new FakeBranchAnalysisService();
        var secondService = new TrackWorkflowService(
            analysis,
            branches,
            new TrackWorkflowServiceOptions
            {
                WorkspaceRoot = pathProvider.WorkflowCacheDirectory,
                FileIdentityService = identityService,
                RuntimePackageCacheService = cacheService,
                Tuning = runtimeOnlyTuning
            });

        TrackWorkflowResult result = await secondService.RunAsync(new TrackWorkflowRequest(input, correlationId: "runtime-only"));

        result.CacheHit.Should().BeTrue();
        result.AnalysisSource.Should().Be("Cached branches, rebuilt runtime tuning");
        result.RuntimePackage.Should().NotBeNull();
        result.RuntimePackage!.BranchDecisionOptions.JumpProbability.Should().Be(0.91);
        result.RuntimePackage.BranchDecisionOptions.JumpCooldownBeats.Should().Be(3);
        result.RuntimePackage.BranchDecisionOptions.FirstPassLinearPlaybackRatio.Should().Be(0.2);
        result.RuntimePackage.Files.RunRoot.Should().EndWith("runtime-only");
        analysis.Requests.Should().BeEmpty();
        branches.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsyncShouldNotUseCompatibleBranchCacheWhenBranchTuningChanges()
    {
        string tempRoot = Directory.CreateTempSubdirectory("eternalloop-core-cache-branch-change-").FullName;
        string audioPath = Path.Combine(tempRoot, "track.mp3");
        await File.WriteAllBytesAsync(audioPath, [1, 2, 3, 4]);
        var pathProvider = new AppPathProvider(Path.Combine(tempRoot, "appdata"));
        var identityService = new TrackFileIdentityService();
        var cacheService = new TrackRuntimePackageCacheService(pathProvider);

        var firstService = new TrackWorkflowService(
            new FakeAnalysisEngineService(CreateAnalysis(audioPath)),
            new FakeBranchAnalysisService(),
            new TrackWorkflowServiceOptions
            {
                WorkspaceRoot = pathProvider.WorkflowCacheDirectory,
                FileIdentityService = identityService,
                RuntimePackageCacheService = cacheService
            });

        TrackInput input = TrackInput.FromFilePath(audioPath);
        await firstService.RunAsync(new TrackWorkflowRequest(input, correlationId: "fresh"));

        LoopTuningSettings changedTuning = LoopTuningSettings.Balanced();
        changedTuning.SimilarityThreshold = 0.8;
        var analysis = new FakeAnalysisEngineService(CreateAnalysis(audioPath));
        var branches = new FakeBranchAnalysisService();
        var secondService = new TrackWorkflowService(
            analysis,
            branches,
            new TrackWorkflowServiceOptions
            {
                WorkspaceRoot = pathProvider.WorkflowCacheDirectory,
                FileIdentityService = identityService,
                RuntimePackageCacheService = cacheService,
                Tuning = changedTuning
            });

        TrackWorkflowResult result = await secondService.RunAsync(new TrackWorkflowRequest(input, correlationId: "branch-change"));

        result.CacheHit.Should().BeFalse();
        result.AnalysisSource.Should().Be("Fresh analysis");
        analysis.Requests.Should().ContainSingle();
        branches.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task RunAsyncShouldNotUseCompatibleBranchCacheWhenAnalysisTuningChanges()
    {
        string tempRoot = Directory.CreateTempSubdirectory("eternalloop-core-cache-analysis-change-").FullName;
        string audioPath = Path.Combine(tempRoot, "track.mp3");
        await File.WriteAllBytesAsync(audioPath, [1, 2, 3, 4]);
        var pathProvider = new AppPathProvider(Path.Combine(tempRoot, "appdata"));
        var identityService = new TrackFileIdentityService();
        var cacheService = new TrackRuntimePackageCacheService(pathProvider);

        var firstService = new TrackWorkflowService(
            new FakeAnalysisEngineService(CreateAnalysis(audioPath)),
            new FakeBranchAnalysisService(),
            new TrackWorkflowServiceOptions
            {
                WorkspaceRoot = pathProvider.WorkflowCacheDirectory,
                FileIdentityService = identityService,
                RuntimePackageCacheService = cacheService
            });

        TrackInput input = TrackInput.FromFilePath(audioPath);
        await firstService.RunAsync(new TrackWorkflowRequest(input, correlationId: "fresh"));

        LoopTuningSettings changedTuning = LoopTuningSettings.Balanced();
        changedTuning.AnalysisMusicalQuality = false;
        var analysis = new FakeAnalysisEngineService(CreateAnalysis(audioPath));
        var branches = new FakeBranchAnalysisService();
        var secondService = new TrackWorkflowService(
            analysis,
            branches,
            new TrackWorkflowServiceOptions
            {
                WorkspaceRoot = pathProvider.WorkflowCacheDirectory,
                FileIdentityService = identityService,
                RuntimePackageCacheService = cacheService,
                Tuning = changedTuning
            });

        TrackWorkflowResult result = await secondService.RunAsync(new TrackWorkflowRequest(input, correlationId: "analysis-change"));

        result.CacheHit.Should().BeFalse();
        result.AnalysisSource.Should().Be("Fresh analysis");
        analysis.Requests.Should().ContainSingle();
        branches.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task RunAsyncShouldIgnoreCacheWhenForceReanalysisIsTrue()
    {
        string tempRoot = Directory.CreateTempSubdirectory("eternalloop-core-cache-force-").FullName;
        string audioPath = Path.Combine(tempRoot, "track.mp3");
        await File.WriteAllBytesAsync(audioPath, [1, 2, 3, 4]);
        var pathProvider = new AppPathProvider(Path.Combine(tempRoot, "appdata"));
        var identityService = new TrackFileIdentityService();
        var cacheService = new TrackRuntimePackageCacheService(pathProvider);

        var firstService = new TrackWorkflowService(
            new FakeAnalysisEngineService(CreateAnalysis(audioPath)),
            new FakeBranchAnalysisService(),
            new TrackWorkflowServiceOptions
            {
                WorkspaceRoot = pathProvider.WorkflowCacheDirectory,
                FileIdentityService = identityService,
                RuntimePackageCacheService = cacheService
            });
        TrackInput input = TrackInput.FromFilePath(audioPath);
        await firstService.RunAsync(new TrackWorkflowRequest(input, correlationId: "fresh"));

        var analysis = new FakeAnalysisEngineService(CreateAnalysis(audioPath));
        var branches = new FakeBranchAnalysisService();
        var forcedService = new TrackWorkflowService(
            analysis,
            branches,
            new TrackWorkflowServiceOptions
            {
                WorkspaceRoot = pathProvider.WorkflowCacheDirectory,
                FileIdentityService = identityService,
                RuntimePackageCacheService = cacheService
            });

        TrackWorkflowResult result = await forcedService.RunAsync(new TrackWorkflowRequest(input, forceReanalysis: true, correlationId: "forced"));

        result.CacheHit.Should().BeFalse();
        analysis.Requests.Should().ContainSingle();
        branches.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task RunAsyncShouldReturnCanceledWhenTokenIsPreCanceled()
    {
        string tempRoot = Directory.CreateTempSubdirectory("eternalloop-core-workflow-").FullName;
        string audioPath = Path.Combine(tempRoot, "track.mp3");
        await File.WriteAllBytesAsync(audioPath, [1, 2, 3, 4]);

        var service = new TrackWorkflowService(
            new FakeAnalysisEngineService(CreateAnalysis(audioPath)),
            new FakeBranchAnalysisService());

        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        var result = await service.RunAsync(
            new TrackWorkflowRequest(TrackInput.FromFilePath(audioPath)),
            cancellationToken: cts.Token);

        result.Status.Should().Be(TrackWorkflowStatus.Canceled);
    }

    [Fact]
    public async Task RunAsyncShouldReportProgress()
    {
        string tempRoot = Directory.CreateTempSubdirectory("eternalloop-core-workflow-").FullName;
        string audioPath = Path.Combine(tempRoot, "track.mp3");
        await File.WriteAllBytesAsync(audioPath, [1, 2, 3, 4]);
        var progressReporter = new CapturingTrackWorkflowProgressReporter();

        var service = new TrackWorkflowService(
            new FakeAnalysisEngineService(CreateAnalysis(audioPath)),
            new FakeBranchAnalysisService(),
            new TrackWorkflowServiceOptions
            {
                WorkspaceRoot = Path.Combine(tempRoot, "workspace")
            });

        await service.RunAsync(
            new TrackWorkflowRequest(TrackInput.FromFilePath(audioPath), correlationId: "progress-run"),
            progressReporter);

        progressReporter.Progress.Should().Contain(progress => progress.Status == TrackWorkflowStatus.Queued);
        progressReporter.Progress.Should().Contain(progress => progress.Status == TrackWorkflowStatus.ValidatingInput);
        progressReporter.Progress.Should().Contain(progress => progress.Status == TrackWorkflowStatus.AnalyzingAudio);
        progressReporter.Progress.Should().Contain(progress => progress.Status == TrackWorkflowStatus.BuildingBranches);
        progressReporter.Progress.Should().Contain(progress => progress.Status == TrackWorkflowStatus.PreparingRuntime);
        progressReporter.Progress.Should().Contain(progress => progress.Status == TrackWorkflowStatus.Completed);
    }

    [Fact]
    public void ConstructorShouldRejectNullAnalysisEngineService()
    {
        var act = () => new TrackWorkflowService(
            null!,
            new FakeBranchAnalysisService());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ConstructorShouldRejectNullBranchAnalysisService()
    {
        var act = () => new TrackWorkflowService(
            new FakeAnalysisEngineService(CreateAnalysis()),
            null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class FakeAnalysisEngineService : IAnalysisEngineService
    {
        private readonly TrackAnalysis _analysis;

        public FakeAnalysisEngineService(TrackAnalysis analysis)
        {
            _analysis = analysis;
        }

        public List<AnalysisEngineRequest> Requests { get; } = [];

        public Task<AnalysisEngineResult> AnalyzeAsync(
            AnalysisEngineRequest request,
            IAnalysisProgressReporter? progressReporter = null,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            progressReporter?.Report(AnalysisStage.LoadingAudio, 0.1, "Loading audio.");
            progressReporter?.Report(AnalysisStage.Done, 1, "Done.");

            return Task.FromResult(new AnalysisEngineResult(_analysis));
        }
    }

    private sealed class FakeBranchAnalysisService : IBranchAnalysisService
    {
        public List<BranchAnalysisRequest> Requests { get; } = [];

        public Task<ApplicationBranchAnalysisResult> AnalyzeAsync(
            BranchAnalysisRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            File.Exists(request.AnalysisPath).Should().BeTrue();
            Directory.Exists(request.OutputRoot).Should().BeTrue();
            string itemRoot = Path.Combine(request.OutputRoot, request.AnalysisName);
            Directory.CreateDirectory(itemRoot);
            string outputPath = Path.Combine(itemRoot, "eternalloop-branches.json");

            var payload = new BranchExportPayload
            {
                SchemaVersion = "branch-export-0.1.0",
                Track = new BranchExportTrack
                {
                    Id = "track-id",
                    Title = request.AnalysisName,
                    Artist = "Local",
                    Duration = 60
                },
                Counts = new BranchExportCounts
                {
                    Beats = 2,
                    Segments = 1,
                    ActiveBranches = 3,
                    CandidateBranches = 5
                },
                ActiveBranches =
                [
                    CreateBranch(1, "active", 0, 1, 0.1),
                    CreateBranch(2, "active", 1, 0, 0.2),
                    CreateBranch(3, "active", 0, 1, 0.3)
                ],
                CandidateBranches =
                [
                    CreateBranch(4, "candidate", 0, 1, 0.4),
                    CreateBranch(5, "candidate", 1, 0, 0.5),
                    CreateBranch(6, "candidate", 0, 1, 0.6),
                    CreateBranch(7, "candidate", 1, 0, 0.7),
                    CreateBranch(8, "candidate", 0, 1, 0.8)
                ]
            };

            File.WriteAllText(outputPath, JsonSerializer.Serialize(payload));

            return Task.FromResult(new ApplicationBranchAnalysisResult(
                new BranchAnalysisItemResult
                {
                    Name = request.AnalysisName,
                    TrackId = "track-id",
                    Beats = 2,
                    Segments = 1,
                    ActiveBranches = 3,
                    CandidateBranches = 5,
                    OutputPath = outputPath
                }));
        }

        private static BranchExportBranch CreateBranch(
            int id,
            string status,
            int fromBeat,
            int toBeat,
            double distance)
        {
            return new BranchExportBranch
            {
                Id = id,
                Status = status,
                FromBeat = fromBeat,
                ToBeat = toBeat,
                JumpBeats = toBeat - fromBeat,
                Direction = toBeat < fromBeat ? "backward" : "forward",
                Distance = distance
            };
        }
    }

    private sealed class CapturingTrackWorkflowProgressReporter : ITrackWorkflowProgressReporter
    {
        public List<TrackWorkflowProgress> Progress { get; } = [];

        public ValueTask ReportAsync(
            TrackWorkflowProgress progress,
            CancellationToken cancellationToken = default)
        {
            Progress.Add(progress);
            return ValueTask.CompletedTask;
        }
    }

    private static TrackAnalysis CreateAnalysis(string filePath = "track.mp3")
    {
        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = "hash",
                FilePath = filePath,
                DurationSeconds = 60,
                SampleRate = 22050,
                Tempo = 120,
                TimeSignature = 4,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion
            },
            Beats =
            [
                new Beat
                {
                    Index = 0,
                    Start = 0,
                    Duration = 0.5,
                    Confidence = 1,
                    Timbre = [0],
                    Pitches = [0],
                    Loudness = [0],
                    BarPosition = [1, 0, 0, 0]
                },
                new Beat
                {
                    Index = 1,
                    Start = 0.5,
                    Duration = 0.5,
                    Confidence = 1,
                    Timbre = [0],
                    Pitches = [0],
                    Loudness = [0],
                    BarPosition = [0, 1, 0, 0]
                }
            ],
            Bars =
            [
                new Bar
                {
                    Index = 0,
                    Start = 0,
                    Duration = 1,
                    Confidence = 1
                }
            ],
            Tatums =
            [
                new Tatum { Index = 0, Start = 0, Duration = 0.25, Confidence = 1 },
                new Tatum { Index = 1, Start = 0.25, Duration = 0.25, Confidence = 1 }
            ],
            Segments =
            [
                new Segment
                {
                    Start = 0,
                    Duration = 1,
                    Confidence = 1,
                    LoudnessStart = -10,
                    LoudnessMax = -5,
                    LoudnessMaxTime = 0.1,
                    Timbre = [0],
                    Pitches = [0]
                }
            ],
            Sections =
            [
                new Section
                {
                    Index = 0,
                    Start = 0,
                    Duration = 60,
                    Confidence = 1,
                    Loudness = -5,
                    Tempo = 120,
                    Label = "A"
                }
            ]
        };
    }
}
