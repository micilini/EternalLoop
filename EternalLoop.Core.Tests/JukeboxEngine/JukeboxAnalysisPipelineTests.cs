using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.JukeboxEngine;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalLoop.Core.Tests.JukeboxEngine;

public sealed class JukeboxAnalysisPipelineTests
{
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
    public async Task AnalyzeAsync_Reports_All_Expected_Stages()
    {
        var pipeline = CreatePipeline();
        var progress = new RecordingProgressReporter();

        await pipeline.AnalyzeAsync("test.wav", progress, CancellationToken.None);

        progress.Stages.Should().Contain(AnalysisStage.Loading);
        progress.Stages.Should().Contain(AnalysisStage.ExtractingFeatures);
        progress.Stages.Should().Contain(AnalysisStage.TrackingBeats);
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
        FakeTrackAnalysisCache? cache = null)
    {
        return new JukeboxAnalysisPipeline(
            audioLoader ?? new FakeAudioLoader(),
            featureExtractor ?? new FakeFeatureExtractor(),
            beatTracker ?? new FakeBeatTracker(),
            branchFinder ?? new FakeBranchFinder(),
            cache ?? new FakeTrackAnalysisCache(),
            NullLogger<JukeboxAnalysisPipeline>.Instance);
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

    private static TrackAnalysis CreateCachedAnalysis()
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
            Sections = []
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

    private sealed class RecordingProgressReporter : IAnalysisProgressReporter
    {
        public List<AnalysisStage> Stages { get; } = [];

        public void Report(AnalysisStage stage, double progress01, string? message = null)
        {
            Stages.Add(stage);
        }
    }
}
