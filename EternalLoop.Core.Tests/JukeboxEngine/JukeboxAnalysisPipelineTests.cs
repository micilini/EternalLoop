using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.AI;
using EternalLoop.Core.Diagnostics;
using EternalLoop.Core.JukeboxEngine;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalLoop.Core.Tests.JukeboxEngine;

public sealed class JukeboxAnalysisPipelineTests
{
    private static readonly string DiagnosticsRoot = Path.Combine(Path.GetTempPath(), "EternalLoopPipelineDiagnostics", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task AnalyzeAsync_Loads_Audio_Extracts_Features_Tracks_Beats_And_Builds_Graph()
    {
        var audioLoader = new FakeAudioLoader();
        var featureExtractor = new FakeFeatureExtractor();
        var beatTracker = new FakeBeatTracker();
        var branchFinder = new FakeBranchFinder();
        var cache = new FakeTrackAnalysisCache();
        var pipeline = CreatePipeline(audioLoader, featureExtractor, beatTracker, branchFinder, cache);

        await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        audioLoader.WasCalled.Should().BeTrue();
        featureExtractor.WasCalled.Should().BeTrue();
        beatTracker.WasCalled.Should().BeTrue();
        branchFinder.WasCalled.Should().BeTrue();
        cache.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task AnalyzeAsync_SavesAiData_WhenAiSimilarityIsEnabled()
    {
        var cache = new FakeTrackAnalysisCache();
        var pipeline = CreatePipeline(cache: cache);

        await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        cache.Saved.Should().NotBeNull();
        cache.Saved!.Ai.Should().NotBeNull();
        cache.Saved.Ai!.BeatEmbeddings.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsCompletedAiRun_WhenAiSucceeds()
    {
        var pipeline = CreatePipeline();

        var result = await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        result.AiRun.Status.Should().Be(AiAnalysisRunStatus.Completed);
        result.AiRun.UsedAi.Should().BeTrue();
        result.AiRun.FellBackToClassic.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeAsync_SkipsAiData_WhenAiSimilarityIsDisabled()
    {
        var aiExtractor = new FakeAiEmbeddingExtractor();
        var cache = new FakeTrackAnalysisCache();
        var pipeline = CreatePipeline(cache: cache, aiEmbeddingExtractor: aiExtractor);

        var result = await pipeline.AnalyzeAsync(
            "test.wav",
            new RecordingProgressReporter(),
            CancellationToken.None,
            branchOptions: CreateBranchOptions(useAiSimilarity: false));

        aiExtractor.CallCount.Should().Be(0);
        cache.Saved.Should().NotBeNull();
        cache.Saved!.Ai.Should().BeNull();
        result.AiRun.Status.Should().Be(AiAnalysisRunStatus.Disabled);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsDisabledAiRun_WhenAiSimilarityIsDisabled()
    {
        var pipeline = CreatePipeline();

        var result = await pipeline.AnalyzeAsync(
            "test.wav",
            new RecordingProgressReporter(),
            CancellationToken.None,
            branchOptions: CreateBranchOptions(useAiSimilarity: false));

        result.AiRun.Status.Should().Be(AiAnalysisRunStatus.Disabled);
        result.AiRun.UsedAi.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeAsync_ReportsRunningAi_WhenAiSimilarityIsEnabled()
    {
        var pipeline = CreatePipeline();
        var progress = new RecordingProgressReporter();

        await pipeline.AnalyzeAsync("test.wav", progress, CancellationToken.None);

        progress.Stages.Should().Contain(AnalysisStage.RunningAi);
    }

    [Fact]
    public async Task AnalyzeAsync_DoesNotReportRunningAi_WhenAiSimilarityIsDisabled()
    {
        var pipeline = CreatePipeline();
        var progress = new RecordingProgressReporter();

        await pipeline.AnalyzeAsync(
            "test.wav",
            progress,
            CancellationToken.None,
            branchOptions: CreateBranchOptions(useAiSimilarity: false));

        progress.Stages.Should().NotContain(AnalysisStage.RunningAi);
    }

    [Fact]
    public async Task AnalyzeAsync_Reports_All_Expected_Stages()
    {
        var pipeline = CreatePipeline();
        var progress = new RecordingProgressReporter();

        await pipeline.AnalyzeAsync("test.wav", progress, CancellationToken.None);

        progress.Stages.Should().Contain(AnalysisStage.Loading);
        progress.Stages.Should().Contain(AnalysisStage.ExtractingFeatures);
        progress.Stages.Should().Contain(AnalysisStage.TrackingBeats);
        progress.Stages.Should().Contain(AnalysisStage.RunningAi);
        progress.Stages.Should().Contain(AnalysisStage.BuildingGraph);
        progress.Stages.Should().Contain(AnalysisStage.Done);
    }

    [Fact]
    public async Task AnalyzeAsync_Returns_LoadedAudio_TrackAnalysis_And_Graph()
    {
        var pipeline = CreatePipeline();

        var result = await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        result.Audio.Should().NotBeNull();
        result.Analysis.Should().NotBeNull();
        result.Graph.Should().NotBeNull();
        result.Analysis.Beats.Should().NotBeEmpty();
        result.Graph.Nodes.Should().HaveCount(result.Analysis.Beats.Count);
        result.LoadedFromCache.Should().BeFalse();
    }

    [Fact]
    public void BuildGraph_Uses_BranchFinder_And_GraphBuilder()
    {
        var branchFinder = new FakeBranchFinder();
        var pipeline = CreatePipeline(branchFinder: branchFinder);
        var beats = CreateBeats();

        var graph = pipeline.BuildGraph(beats, new BranchFindingOptions());

        branchFinder.WasCalled.Should().BeTrue();
        graph.Nodes.Should().HaveCount(beats.Length);
        graph.JumpEdges.Should().ContainKey(0);
    }

    [Fact]
    public async Task AnalyzeAsync_UsesCache_WhenAnalysisExists()
    {
        var audioLoader = new FakeAudioLoader();
        var featureExtractor = new FakeFeatureExtractor();
        var beatTracker = new FakeBeatTracker();
        var cache = new FakeTrackAnalysisCache { Cached = CreateCachedAnalysis() };
        var pipeline = CreatePipeline(audioLoader, featureExtractor, beatTracker, cache: cache);

        var result = await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        audioLoader.WasCalled.Should().BeTrue();
        featureExtractor.WasCalled.Should().BeFalse();
        beatTracker.WasCalled.Should().BeFalse();
        cache.TryGetCalls.Should().Be(1);
        cache.SaveCalls.Should().Be(0);
        result.LoadedFromCache.Should().BeTrue();
        result.Graph.Nodes.Should().HaveCount(result.Analysis.Beats.Count);
    }

    [Fact]
    public async Task AnalyzeAsync_UsesCachedAnalysisWithCompatibleAi_WhenAiSimilarityIsEnabled()
    {
        var aiExtractor = new FakeAiEmbeddingExtractor();
        var cache = new FakeTrackAnalysisCache { Cached = CreateCachedAnalysis(includeAi: true) };
        var pipeline = CreatePipeline(cache: cache, aiEmbeddingExtractor: aiExtractor);

        var result = await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        result.LoadedFromCache.Should().BeTrue();
        aiExtractor.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsLoadedFromCacheAiRun_WhenCompatibleAiCacheIsUsed()
    {
        var aiExtractor = new FakeAiEmbeddingExtractor();
        var cache = new FakeTrackAnalysisCache { Cached = CreateCachedAnalysis(includeAi: true) };
        var pipeline = CreatePipeline(cache: cache, aiEmbeddingExtractor: aiExtractor);

        var result = await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        result.AiRun.Status.Should().Be(AiAnalysisRunStatus.LoadedFromCache);
        result.AiRun.UsedAi.Should().BeTrue();
        aiExtractor.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeAsync_IgnoresCachedAnalysisWithoutAi_WhenAiSimilarityIsEnabled()
    {
        var aiExtractor = new FakeAiEmbeddingExtractor();
        var cache = new FakeTrackAnalysisCache { Cached = CreateCachedAnalysis(includeAi: false) };
        var pipeline = CreatePipeline(cache: cache, aiEmbeddingExtractor: aiExtractor);

        var result = await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        result.LoadedFromCache.Should().BeFalse();
        aiExtractor.CallCount.Should().Be(1);
        cache.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task AnalyzeAsync_UsesCachedAnalysisWithoutAi_WhenAiSimilarityIsDisabled()
    {
        var aiExtractor = new FakeAiEmbeddingExtractor();
        var cache = new FakeTrackAnalysisCache { Cached = CreateCachedAnalysis(includeAi: false) };
        var pipeline = CreatePipeline(cache: cache, aiEmbeddingExtractor: aiExtractor);

        var result = await pipeline.AnalyzeAsync(
            "test.wav",
            new RecordingProgressReporter(),
            CancellationToken.None,
            branchOptions: CreateBranchOptions(useAiSimilarity: false));

        result.LoadedFromCache.Should().BeTrue();
        aiExtractor.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeAsync_DoesNotExtractAiAgain_WhenCompatibleAiCacheExists()
    {
        var aiExtractor = new FakeAiEmbeddingExtractor();
        var cache = new FakeTrackAnalysisCache { Cached = CreateCachedAnalysis(includeAi: true) };
        var pipeline = CreatePipeline(cache: cache, aiEmbeddingExtractor: aiExtractor);

        await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        aiExtractor.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeAsync_continues_with_classic_analysis_when_ai_throws_index_error()
    {
        var aiExtractor = new FakeAiEmbeddingExtractor
        {
            ExceptionToThrow = new IndexOutOfRangeException()
        };
        var cache = new FakeTrackAnalysisCache();
        var pipeline = CreatePipeline(cache: cache, aiEmbeddingExtractor: aiExtractor);

        var result = await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        result.Analysis.Ai.Should().BeNull();
        result.Graph.Should().NotBeNull();
        cache.SaveCalls.Should().Be(1);
        result.LoadedFromCache.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsFailedFallbackAiRun_WhenAiFails()
    {
        var aiExtractor = new FakeAiEmbeddingExtractor
        {
            ExceptionToThrow = new IndexOutOfRangeException()
        };
        var pipeline = CreatePipeline(aiEmbeddingExtractor: aiExtractor);

        var result = await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        result.AiRun.Status.Should().Be(AiAnalysisRunStatus.FailedFallback);
        result.AiRun.UsedAi.Should().BeFalse();
        result.AiRun.FellBackToClassic.Should().BeTrue();
        result.AiRun.FailureReason.Should().Contain(nameof(IndexOutOfRangeException));
        result.Analysis.Ai.Should().BeNull();
    }

    [Fact]
    public async Task AnalyzeAsync_saves_analysis_without_ai_when_ai_fails()
    {
        var aiExtractor = new FakeAiEmbeddingExtractor
        {
            ExceptionToThrow = new IndexOutOfRangeException()
        };
        var cache = new FakeTrackAnalysisCache();
        var pipeline = CreatePipeline(cache: cache, aiEmbeddingExtractor: aiExtractor);

        await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        cache.Saved.Should().NotBeNull();
        cache.Saved!.Ai.Should().BeNull();
    }

    [Fact]
    public async Task AnalyzeAsync_DoesNotPersistAiData_WhenAiFails()
    {
        var aiExtractor = new FakeAiEmbeddingExtractor
        {
            ExceptionToThrow = new IndexOutOfRangeException()
        };
        var cache = new FakeTrackAnalysisCache();
        var pipeline = CreatePipeline(cache: cache, aiEmbeddingExtractor: aiExtractor);

        var result = await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        result.Analysis.Ai.Should().BeNull();
        cache.Saved.Should().NotBeNull();
        cache.Saved!.Ai.Should().BeNull();
    }

    [Fact]
    public async Task AnalyzeAsync_does_not_swallow_cancellation_when_ai_is_cancelled()
    {
        var aiExtractor = new FakeAiEmbeddingExtractor
        {
            ExceptionToThrow = new OperationCanceledException()
        };
        var pipeline = CreatePipeline(aiEmbeddingExtractor: aiExtractor);

        var act = async () => await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AnalyzeAsync_DoesNotSwallowCancellation_WhenAiIsCancelled()
    {
        var aiExtractor = new FakeAiEmbeddingExtractor
        {
            ExceptionToThrow = new OperationCanceledException()
        };
        var pipeline = CreatePipeline(aiEmbeddingExtractor: aiExtractor);

        var act = async () => await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AnalyzeAsync_reports_classic_fallback_when_ai_fails()
    {
        var aiExtractor = new FakeAiEmbeddingExtractor
        {
            ExceptionToThrow = new IndexOutOfRangeException()
        };
        var pipeline = CreatePipeline(aiEmbeddingExtractor: aiExtractor);
        var progress = new RecordingProgressReporter();

        await pipeline.AnalyzeAsync("test.wav", progress, CancellationToken.None);

        progress.Messages.Should().Contain("AI similarity failed. Using classic analysis.");
        progress.Stages.Should().Contain(AnalysisStage.Done);
    }

    [Fact]
    public async Task AnalysisPipeline_writes_ai_failure_diagnostic_when_ai_falls_back()
    {
        var aiExtractor = new FakeAiEmbeddingExtractor
        {
            ThrowNestedIndexException = true
        };
        var pipeline = CreatePipeline(aiEmbeddingExtractor: aiExtractor);

        var result = await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        result.AiRun.Status.Should().Be(AiAnalysisRunStatus.FailedFallback);
        result.AiRun.DiagnosticFilePath.Should().NotBeNullOrWhiteSpace();
        File.Exists(result.AiRun.DiagnosticFilePath).Should().BeTrue();
        var text = File.ReadAllText(result.AiRun.DiagnosticFilePath!);
        text.Should().Contain(nameof(IndexOutOfRangeException));
        text.Should().Contain("Synthetic pipeline AI index failure.");
        text.Should().Contain(nameof(ThrowNestedException));
        text.Should().Contain("Exception.ToString()");
        result.Analysis.Ai.Should().BeNull();
        result.Graph.Should().NotBeNull();
    }


    [Fact]
    public async Task AnalyzeAsync_SavesAnalysis_WhenCacheMisses()
    {
        var cache = new FakeTrackAnalysisCache();
        var pipeline = CreatePipeline(cache: cache);

        await pipeline.AnalyzeAsync("test.wav", new RecordingProgressReporter(), CancellationToken.None);

        cache.TryGetCalls.Should().Be(1);
        cache.SaveCalls.Should().Be(1);
        cache.Saved.Should().NotBeNull();
    }

    [Fact]
    public async Task AnalyzeAsync_UsesCustomBranchOptions_OnCacheHit()
    {
        var branchFinder = new FakeBranchFinder();
        var cache = new FakeTrackAnalysisCache { Cached = CreateCachedAnalysis() };
        var pipeline = CreatePipeline(branchFinder: branchFinder, cache: cache);
        var options = new BranchFindingOptions
        {
            SimilarityThreshold = 0.72,
            LookaheadDepth = 2,
            MinJumpDistance = 8,
            MaxBranchesPerBeat = 8
        };

        await pipeline.AnalyzeAsync(
            "test.wav",
            new RecordingProgressReporter(),
            CancellationToken.None,
            branchOptions: options);

        branchFinder.LastOptions.Should().NotBeNull();
        branchFinder.LastOptions!.SimilarityThreshold.Should().Be(0.72);
        branchFinder.LastOptions.LookaheadDepth.Should().Be(2);
        branchFinder.LastOptions.MinJumpDistance.Should().Be(8);
        branchFinder.LastOptions.MaxBranchesPerBeat.Should().Be(8);
    }

    [Fact]
    public async Task AnalyzeAsync_UsesCustomBranchOptions_OnFreshAnalysis()
    {
        var branchFinder = new FakeBranchFinder();
        var pipeline = CreatePipeline(branchFinder: branchFinder);
        var options = new BranchFindingOptions
        {
            SimilarityThreshold = 0.9,
            LookaheadDepth = 4,
            MinJumpDistance = 24,
            MaxBranchesPerBeat = 3
        };

        await pipeline.AnalyzeAsync(
            "test.wav",
            new RecordingProgressReporter(),
            CancellationToken.None,
            branchOptions: options);

        branchFinder.LastOptions.Should().NotBeNull();
        branchFinder.LastOptions!.SimilarityThreshold.Should().Be(0.9);
        branchFinder.LastOptions.LookaheadDepth.Should().Be(4);
        branchFinder.LastOptions.MinJumpDistance.Should().Be(24);
        branchFinder.LastOptions.MaxBranchesPerBeat.Should().Be(3);
    }

    [Fact]
    public async Task AnalyzeAsync_BypassesCache_WhenForceReanalysisIsTrue()
    {
        var audioLoader = new FakeAudioLoader();
        var featureExtractor = new FakeFeatureExtractor();
        var beatTracker = new FakeBeatTracker();
        var cache = new FakeTrackAnalysisCache { Cached = CreateCachedAnalysis() };
        var pipeline = CreatePipeline(audioLoader, featureExtractor, beatTracker, cache: cache);

        var result = await pipeline.AnalyzeAsync(
            "test.wav",
            new RecordingProgressReporter(),
            CancellationToken.None,
            forceReanalysis: true);

        audioLoader.WasCalled.Should().BeTrue();
        featureExtractor.WasCalled.Should().BeTrue();
        beatTracker.WasCalled.Should().BeTrue();
        cache.TryGetCalls.Should().Be(0);
        cache.SaveCalls.Should().Be(1);
        result.LoadedFromCache.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeAsync_IgnoresSuspiciousCachedBeatDensity()
    {
        var audioLoader = new FakeAudioLoader { DurationSeconds = 252.34 };
        var featureExtractor = new FakeFeatureExtractor();
        var beatTracker = new FakeBeatTracker();
        var cache = new FakeTrackAnalysisCache { Cached = CreateSparseCachedAnalysis() };
        var pipeline = CreatePipeline(audioLoader, featureExtractor, beatTracker, cache: cache);

        var result = await pipeline.AnalyzeAsync(
            "gangnam.mp3",
            new RecordingProgressReporter(),
            CancellationToken.None);

        cache.TryGetCalls.Should().Be(1);
        featureExtractor.WasCalled.Should().BeTrue();
        beatTracker.WasCalled.Should().BeTrue();
        cache.SaveCalls.Should().Be(1);
        result.LoadedFromCache.Should().BeFalse();
    }

    private static JukeboxAnalysisPipeline CreatePipeline(
        FakeAudioLoader? audioLoader = null,
        FakeFeatureExtractor? featureExtractor = null,
        FakeBeatTracker? beatTracker = null,
        FakeBranchFinder? branchFinder = null,
        FakeTrackAnalysisCache? cache = null,
        FakeAiEmbeddingExtractor? aiEmbeddingExtractor = null)
    {
        return new JukeboxAnalysisPipeline(
            audioLoader ?? new FakeAudioLoader(),
            featureExtractor ?? new FakeFeatureExtractor(),
            beatTracker ?? new FakeBeatTracker(),
            branchFinder ?? new FakeBranchFinder(),
            cache ?? new FakeTrackAnalysisCache(),
            aiEmbeddingExtractor ?? new FakeAiEmbeddingExtractor(),
            new AiBeatEmbeddingAggregator(),
            CreateDiagnosticWriter(),
            NullLogger<JukeboxAnalysisPipeline>.Instance);
    }

    private static AiFailureDiagnosticWriter CreateDiagnosticWriter()
    {
        return new AiFailureDiagnosticWriter(
            new FakeAppPathProvider(DiagnosticsRoot),
            NullLogger<AiFailureDiagnosticWriter>.Instance);
    }

    private static Beat[] CreateBeats()
    {
        return Enumerable.Range(0, 4)
            .Select(index => new Beat
            {
                Index = index,
                Start = index * 0.5,
                Duration = 0.5,
                Confidence = 1.0,
                Timbre = [1f],
                Pitches = [1f],
                Loudness = [0f, 0f, 0f],
                BarPosition = [0f, 1f]
            })
            .ToArray();
    }

    private static BranchFindingOptions CreateBranchOptions(bool useAiSimilarity)
    {
        return new BranchFindingOptions
        {
            UseAiSimilarity = useAiSimilarity
        };
    }

    private static TrackAnalysis CreateCachedAnalysis(bool includeAi = true)
    {
        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = "hash",
                FilePath = "cached.wav",
                DurationSeconds = 1.0,
                SampleRate = 22_050,
                Tempo = 120,
                TimeSignature = 4,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion
            },
            Segments = [],
            Beats = Enumerable.Range(0, 24)
                .Select(index => new Beat
                {
                    Index = index,
                    Start = index * 0.5,
                    Duration = 0.5,
                    Confidence = 1.0,
                    Timbre = [1f],
                    Pitches = [1f],
                    Loudness = [0f, 0f, 0f],
                    BarPosition = [0f, 1f]
                })
                .ToArray(),
            Bars = [],
            Tatums = [],
            Sections = [],
            Ai = includeAi ? CreateAiAnalysisData() : null
        };
    }

    private static TrackAnalysis CreateSparseCachedAnalysis()
    {
        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = "hash",
                FilePath = "gangnam.mp3",
                DurationSeconds = 252.34,
                SampleRate = 22_050,
                Tempo = 123,
                TimeSignature = 4,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion
            },
            Segments = [],
            Beats = Enumerable.Range(0, 63)
                .Select(index => new Beat
                {
                    Index = index,
                    Start = index * 4.0,
                    Duration = 4.0,
                    Confidence = 1.0,
                    Timbre = [1f],
                    Pitches = [1f],
                    Loudness = [0f, 0f, 0f],
                    BarPosition = [0f, 1f]
                })
                .ToArray(),
            Bars = [],
            Tatums = [],
            Sections = []
        };
    }

    private static AiAnalysisData CreateAiAnalysisData()
    {
        return new AiAnalysisData
        {
            ModelId = AiModelDefaultValues.DiscogsEffNetModelId,
            ModelVersion = AiModelDefaultValues.DiscogsEffNetVersion,
            SampleRate = AiModelDefaultValues.DiscogsEffNetSampleRate,
            EmbeddingDimensions = AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions,
            BeatEmbeddings =
            [
                new AiBeatEmbedding
                {
                    BeatIndex = 0,
                    Vector = CreateEmbeddingVector(1.0f)
                }
            ]
        };
    }

    private sealed class FakeAudioLoader : IAudioLoader
    {
        public bool WasCalled { get; private set; }

        public double DurationSeconds { get; init; } = 1.0;

        public Task<LoadedAudio> LoadAsync(string filePath, int targetSampleRate, CancellationToken cancellationToken)
        {
            WasCalled = true;

            return Task.FromResult(new LoadedAudio(
                Enumerable.Repeat(0.1f, targetSampleRate).ToArray(),
                targetSampleRate,
                DurationSeconds,
                "hash"));
        }
    }

    private sealed class FakeFeatureExtractor : IFeatureExtractor
    {
        public bool WasCalled { get; private set; }

        public FeatureMatrix Extract(LoadedAudio audio, FeatureExtractionOptions options)
        {
            WasCalled = true;

            return new FeatureMatrix
            {
                Mfcc = Enumerable.Range(0, 8).Select(i => new[] { (float)i, 1f }).ToArray(),
                Chroma = Enumerable.Range(0, 8).Select(i => new[] { 1f, (float)i }).ToArray(),
                SpectralFlux = new float[8],
                Rms = new float[8],
                HopLengthSamples = 2_756,
                FrameSizeSamples = 2_048
            };
        }
    }

    private sealed class FakeBeatTracker : IBeatTracker
    {
        public bool WasCalled { get; private set; }

        public BeatTrackingResult Track(LoadedAudio audio, FeatureMatrix features, BeatTrackingOptions options)
        {
            WasCalled = true;

            return new BeatTrackingResult
            {
                EstimatedBpm = 120,
                BeatTimes = [0.0, 0.25, 0.5, 0.75],
                Confidences = [1.0, 0.9, 0.8, 0.7]
            };
        }
    }

    private sealed class FakeBranchFinder : IBranchFinder
    {
        public bool WasCalled { get; private set; }

        public BranchFindingOptions? LastOptions { get; private set; }

        public IReadOnlyList<JukeboxEdge> FindBranches(IReadOnlyList<Beat> beats, BranchFindingOptions options)
        {
            WasCalled = true;
            LastOptions = options;

            return beats.Count >= 3
                ?
                [
                    new JukeboxEdge
                    {
                        FromBeat = beats[0].Index,
                        ToBeat = beats[2].Index,
                        Similarity = 0.9
                    }
                ]
                : [];
        }
    }

    private sealed class FakeTrackAnalysisCache : ITrackAnalysisCache
    {
        public TrackAnalysis? Cached { get; set; }

        public TrackAnalysis? Saved { get; private set; }

        public int TryGetCalls { get; private set; }

        public int SaveCalls { get; private set; }

        public Task<TrackAnalysis?> TryGetAsync(string fileHash, CancellationToken cancellationToken)
        {
            TryGetCalls++;
            return Task.FromResult(Cached);
        }

        public Task SaveAsync(TrackAnalysis analysis, CancellationToken cancellationToken)
        {
            SaveCalls++;
            Saved = analysis;
            return Task.CompletedTask;
        }

        public Task<TrackAnalysisCacheStats> GetStatsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new TrackAnalysisCacheStats());
        }

        public Task ClearAsync()
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAiEmbeddingExtractor : IAiEmbeddingExtractor
    {
        public int CallCount { get; private set; }

        public Exception? ExceptionToThrow { get; init; }

        public bool ThrowNestedIndexException { get; init; }

        public Task<AiEmbeddingExtractionResult> ExtractAsync(
            LoadedAudio audio,
            IAnalysisProgressReporter progressReporter,
            CancellationToken cancellationToken)
        {
            CallCount++;

            if (ThrowNestedIndexException)
            {
                ThrowNestedException();
            }

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(new AiEmbeddingExtractionResult
            {
                ModelId = AiModelDefaultValues.DiscogsEffNetModelId,
                ModelVersion = AiModelDefaultValues.DiscogsEffNetVersion,
                SampleRate = AiModelDefaultValues.DiscogsEffNetSampleRate,
                EmbeddingDimensions = AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions,
                Frames =
                [
                    new AiEmbeddingFrame
                    {
                        Index = 0,
                        Start = 0.0,
                        Duration = 0.5,
                        Vector = CreateEmbeddingVector(1.0f)
                    },
                    new AiEmbeddingFrame
                    {
                        Index = 1,
                        Start = 0.5,
                        Duration = 0.5,
                        Vector = CreateEmbeddingVector(2.0f)
                    }
                ]
            });
        }
    }

    private static float[] CreateEmbeddingVector(float value)
    {
        var vector = new float[AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions];
        vector[0] = value;
        return vector;
    }

    private static void ThrowNestedException()
    {
        throw new IndexOutOfRangeException("Synthetic pipeline AI index failure.");
    }

    private sealed class FakeAppPathProvider : IAppPathProvider
    {
        public FakeAppPathProvider(string root)
        {
            AppDataDirectory = root;
            CacheDirectory = Path.Combine(root, "Cache");
            LogsDirectory = Path.Combine(root, "Logs");
            SettingsFilePath = Path.Combine(root, "settings.json");
        }

        public string AppDataDirectory { get; }

        public string CacheDirectory { get; }

        public string LogsDirectory { get; }

        public string SettingsFilePath { get; }

        public void EnsureDirectories()
        {
            Directory.CreateDirectory(AppDataDirectory);
            Directory.CreateDirectory(CacheDirectory);
            Directory.CreateDirectory(LogsDirectory);
        }
    }

    private sealed class RecordingProgressReporter : IAnalysisProgressReporter
    {
        public List<AnalysisStage> Stages { get; } = [];

        public List<string> Messages { get; } = [];

        public void Report(AnalysisStage stage, double progress01, string? message = null)
        {
            Stages.Add(stage);
            if (message is not null)
            {
                Messages.Add(message);
            }
        }
    }
}
