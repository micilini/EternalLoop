using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.Audio;
using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.Features;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using EternalLoop.AnalysisEngine.Core.Progress;
using EternalLoop.AnalysisEngine.Core.Validation;
using EternalLoop.AnalysisEngine.Tests.TestData;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalLoop.AnalysisEngine.Tests.Analysis;

public sealed class TrackAnalysisPipelineTests
{
    [Fact]
    public async Task AnalyzeAsync_builds_complete_track_analysis()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 4.0);
        var features = CreateFeatureMatrix(frameCount: 64);
        var beatTracking = CreateBeatTrackingResult();
        var pipeline = CreatePipeline(audio, features, beatTracking);

        var analysis = await pipeline.AnalyzeAsync(
            "C:\\Music\\song.wav",
            new AnalysisOptions(),
            NullAnalysisProgressReporter.Instance,
            CancellationToken.None);

        analysis.Metadata.FileHash.Should().Be(audio.FileHash);
        analysis.Metadata.FilePath.Should().Be("C:\\Music\\song.wav");
        analysis.Metadata.DurationSeconds.Should().Be(audio.DurationSeconds);
        analysis.Metadata.SampleRate.Should().Be(audio.SampleRate);
        analysis.Metadata.Tempo.Should().Be(beatTracking.EstimatedBpm);
        analysis.Segments.Should().NotBeEmpty();
        analysis.Segments.Should().HaveCountLessThan(features.Mfcc.Length);
        analysis.Beats.Should().HaveCount(beatTracking.BeatTimes.Length);
        analysis.Bars.Should().NotBeEmpty();
        analysis.Tatums.Should().HaveCount(analysis.Beats.Count * 2);
        analysis.Sections.Should().ContainSingle();
        analysis.MicroFingerprints.Should().BeEmpty();
        analysis.Ai.Should().BeNull();
    }

    [Fact]
    public async Task AnalyzeAsync_reports_expected_stages()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 4.0);
        var features = CreateFeatureMatrix(frameCount: 64);
        var beatTracking = CreateBeatTrackingResult();
        var pipeline = CreatePipeline(audio, features, beatTracking);
        var progress = new RecordingProgressReporter();

        await pipeline.AnalyzeAsync(
            "C:\\Music\\song.wav",
            new AnalysisOptions(),
            progress,
            CancellationToken.None);

        progress.Stages.Should().ContainInOrder(
            AnalysisStage.LoadingAudio,
            AnalysisStage.ExtractingFeatures,
            AnalysisStage.TrackingBeats,
            AnalysisStage.BuildingAnalysis,
            AnalysisStage.Validating,
            AnalysisStage.Done);
    }

    [Fact]
    public async Task AnalyzeAsync_passes_target_sample_rate_to_audio_loader()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 4.0);
        var features = CreateFeatureMatrix(frameCount: 64);
        var beatTracking = CreateBeatTrackingResult();
        var audioLoader = new StubAudioLoader(audio);
        var pipeline = CreatePipeline(audioLoader, features, beatTracking);

        await pipeline.AnalyzeAsync(
            "C:\\Music\\song.wav",
            new AnalysisOptions
            {
                TargetSampleRate = 11025
            },
            NullAnalysisProgressReporter.Instance,
            CancellationToken.None);

        audioLoader.LastTargetSampleRate.Should().Be(11025);
    }

    [Fact]
    public async Task AnalyzeAsync_uses_options_time_signature()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 4.0);
        var features = CreateFeatureMatrix(frameCount: 64);
        var beatTracking = CreateBeatTrackingResult();
        var pipeline = CreatePipeline(audio, features, beatTracking);

        var analysis = await pipeline.AnalyzeAsync(
            "C:\\Music\\song.wav",
            new AnalysisOptions
            {
                TimeSignature = 3
            },
            NullAnalysisProgressReporter.Instance,
            CancellationToken.None);

        analysis.Metadata.TimeSignature.Should().Be(3);
        analysis.Bars.Should().HaveCount(2);
    }

    [Fact]
    public async Task AnalyzeAsync_with_all_musical_quality_flags_false_preserves_default_output()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 4.0);
        var features = CreateFeatureMatrix(frameCount: 64);
        var beatTracking = CreateBeatTrackingResult();
        var pipeline = CreatePipeline(audio, features, beatTracking);

        var baseline = await pipeline.AnalyzeAsync(
            "C:\\Music\\song.wav",
            new AnalysisOptions(),
            NullAnalysisProgressReporter.Instance,
            CancellationToken.None);
        var flaggedOff = await pipeline.AnalyzeAsync(
            "C:\\Music\\song.wav",
            new AnalysisOptions { MusicalQuality = new MusicalQualityOptions() },
            NullAnalysisProgressReporter.Instance,
            CancellationToken.None);

        flaggedOff.Segments.Select(segment => segment.Start).Should().Equal(baseline.Segments.Select(segment => segment.Start));
        flaggedOff.Segments.Select(segment => segment.Duration).Should().Equal(baseline.Segments.Select(segment => segment.Duration));
        flaggedOff.Beats.Select(beat => beat.Start).Should().Equal(baseline.Beats.Select(beat => beat.Start));
        flaggedOff.Tatums.Select(tatum => tatum.Start).Should().Equal(baseline.Tatums.Select(tatum => tatum.Start));
        flaggedOff.Sections.Select(section => section.Start).Should().Equal(baseline.Sections.Select(section => section.Start));
        flaggedOff.Sections.Select(section => section.Duration).Should().Equal(baseline.Sections.Select(section => section.Duration));
        flaggedOff.Diagnostics.Should().NotBeNull();
    }

    [Fact]
    public async Task AnalyzeAsync_with_flags_off_does_not_request_hpss()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 4.0);
        var features = CreateFeatureMatrix(frameCount: 64);
        var beatTracking = CreateBeatTrackingResult();
        var extractor = new StubFeatureExtractor(features);
        var pipeline = CreatePipeline(new StubAudioLoader(audio), extractor, beatTracking);

        await pipeline.AnalyzeAsync(
            "C:\\Music\\song.wav",
            new AnalysisOptions(),
            NullAnalysisProgressReporter.Instance,
            CancellationToken.None);

        extractor.LastOptions.Should().NotBeNull();
        extractor.LastOptions!.Hpss.UseHpss.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeAsync_with_musical_quality_requests_hpss()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 4.0);
        var features = CreateFeatureMatrix(frameCount: 64);
        var beatTracking = CreateBeatTrackingResult();
        var extractor = new StubFeatureExtractor(features);
        var pipeline = CreatePipeline(new StubAudioLoader(audio), extractor, beatTracking);

        await pipeline.AnalyzeAsync(
            "C:\\Music\\song.wav",
            new AnalysisOptions { MusicalQuality = MusicalQualityOptions.AllEnabled },
            NullAnalysisProgressReporter.Instance,
            CancellationToken.None);

        extractor.LastOptions.Should().NotBeNull();
        extractor.LastOptions!.Hpss.UseHpss.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeAsync_with_segmentation_only_keeps_other_fronts_at_default()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 4.0);
        var features = CreateFeatureMatrix(frameCount: 64);
        var beatTracking = CreateBeatTrackingResult();
        var pipeline = CreatePipeline(audio, features, beatTracking);

        var baseline = await pipeline.AnalyzeAsync(
            "C:\\Music\\song.wav",
            new AnalysisOptions(),
            NullAnalysisProgressReporter.Instance,
            CancellationToken.None);
        var segmentationOnly = await pipeline.AnalyzeAsync(
            "C:\\Music\\song.wav",
            new AnalysisOptions
            {
                MusicalQuality = new MusicalQualityOptions { AcousticSegmentation = true }
            },
            NullAnalysisProgressReporter.Instance,
            CancellationToken.None);

        segmentationOnly.Beats.Select(beat => beat.Start).Should().Equal(baseline.Beats.Select(beat => beat.Start));
        segmentationOnly.Tatums.Select(tatum => tatum.Start).Should().Equal(baseline.Tatums.Select(tatum => tatum.Start));
        segmentationOnly.Sections.Select(section => section.Start).Should().Equal(baseline.Sections.Select(section => section.Start));
        segmentationOnly.Diagnostics!.SegmentationMode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AnalyzeAsync_with_all_fronts_handles_short_track()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 0.3);
        var features = CreateFeatureMatrix(frameCount: 8);
        var beatTracking = new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.15],
            Confidences = [0.8, 0.7],
            BeatGridMode = "regular"
        };
        var pipeline = CreatePipeline(audio, features, beatTracking);

        var analysis = await pipeline.AnalyzeAsync(
            "C:\\Music\\short.wav",
            new AnalysisOptions { MusicalQuality = MusicalQualityOptions.AllEnabled },
            NullAnalysisProgressReporter.Instance,
            CancellationToken.None);

        analysis.Segments.Should().NotBeEmpty();
        analysis.Tatums.Should().NotBeEmpty();
        analysis.Sections.Should().NotBeEmpty();
        analysis.Diagnostics.Should().NotBeNull();
        analysis.Diagnostics!.RequestedAcousticSegmentation.Should().BeTrue();
        analysis.Diagnostics.RequestedBeatMicroSnap.Should().BeTrue();
        analysis.Diagnostics.RequestedAdaptiveTatums.Should().BeTrue();
        analysis.Diagnostics.RequestedStructuralSections.Should().BeTrue();
        analysis.Diagnostics.RequestedEvidenceConfidences.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeAsync_throws_when_validation_fails()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 4.0);
        var features = CreateFeatureMatrix(frameCount: 0);
        var beatTracking = CreateBeatTrackingResult();
        var pipeline = CreatePipeline(audio, features, beatTracking);

        var act = () => pipeline.AnalyzeAsync(
            "C:\\Music\\song.wav",
            new AnalysisOptions(),
            NullAnalysisProgressReporter.Instance,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static TrackAnalysisPipeline CreatePipeline(
        LoadedAudio audio,
        FeatureMatrix features,
        BeatTrackingResult beatTracking)
    {
        return CreatePipeline(new StubAudioLoader(audio), features, beatTracking);
    }

    private static TrackAnalysisPipeline CreatePipeline(
        StubAudioLoader audioLoader,
        FeatureMatrix features,
        BeatTrackingResult beatTracking)
    {
        return CreatePipeline(audioLoader, new StubFeatureExtractor(features), beatTracking);
    }

    private static TrackAnalysisPipeline CreatePipeline(
        StubAudioLoader audioLoader,
        StubFeatureExtractor featureExtractor,
        BeatTrackingResult beatTracking)
    {
        return new TrackAnalysisPipeline(
            audioLoader,
            featureExtractor,
            new StubBeatTracker(beatTracking),
            new AnalysisSanityValidator(),
            NullLogger<TrackAnalysisPipeline>.Instance);
    }

    private static BeatTrackingResult CreateBeatTrackingResult()
    {
        return new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [1.0, 0.9, 0.8, 0.7]
        };
    }

    private static FeatureMatrix CreateFeatureMatrix(int frameCount)
    {
        var mfcc = new float[frameCount][];
        var chroma = new float[frameCount][];
        var rms = new float[frameCount];
        var spectralFlux = new float[frameCount];

        for (var frame = 0; frame < frameCount; frame++)
        {
            mfcc[frame] = Enumerable.Range(0, 26).Select(index => (float)(frame + index)).ToArray();
            chroma[frame] = Enumerable.Range(0, 12).Select(index => index == frame % 12 ? 1.0f : 0.0f).ToArray();
            rms[frame] = 0.1f + frame * 0.01f;
            spectralFlux[frame] = frame % 4 == 0 ? 1.0f : 0.0f;
        }

        return new FeatureMatrix
        {
            Mfcc = mfcc,
            Chroma = chroma,
            Rms = rms,
            SpectralFlux = spectralFlux,
            FrameSizeSamples = 2048,
            HopLengthSamples = 512,
            SampleRate = TestSignalFactory.DefaultSampleRate
        };
    }

    private sealed class StubAudioLoader : IAudioLoader
    {
        private readonly LoadedAudio _audio;

        public StubAudioLoader(LoadedAudio audio)
        {
            _audio = audio;
        }

        public int LastTargetSampleRate { get; private set; }

        public Task<LoadedAudio> LoadAsync(
            string filePath,
            int targetSampleRate,
            CancellationToken cancellationToken)
        {
            LastTargetSampleRate = targetSampleRate;
            return Task.FromResult(_audio);
        }
    }

    private sealed class StubFeatureExtractor : IFeatureExtractor
    {
        private readonly FeatureMatrix _features;

        public StubFeatureExtractor(FeatureMatrix features)
        {
            _features = features;
        }

        public FeatureMatrix Extract(LoadedAudio audio, FeatureExtractionOptions options)
        {
            LastOptions = options;
            return _features;
        }

        public FeatureExtractionOptions? LastOptions { get; private set; }
    }

    private sealed class StubBeatTracker : IBeatTracker
    {
        private readonly BeatTrackingResult _result;

        public StubBeatTracker(BeatTrackingResult result)
        {
            _result = result;
        }

        public BeatTrackingResult Track(
            LoadedAudio audio,
            FeatureMatrix features,
            BeatTrackingOptions options)
        {
            return _result;
        }
    }

    private sealed class RecordingProgressReporter : IAnalysisProgressReporter
    {
        private readonly List<AnalysisStage> _stages = [];

        public IReadOnlyList<AnalysisStage> Stages => _stages;

        public void Report(AnalysisStage stage, double progress01, string? message = null)
        {
            _stages.Add(stage);
        }
    }
}
