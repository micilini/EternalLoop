using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai.Advisor;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;
using Xunit;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.BeatThis;

public sealed class BeatThisProviderDownbeatFallbackTests
{
    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; }

        private TemporaryDirectory(string path) => Path = path;

        public static TemporaryDirectory Create()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TemporaryDirectory(path);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch
            {
            }
        }
    }

    private sealed class MockBeatModelRuntime : IBeatModelRuntime
    {
        private readonly IReadOnlyList<int> _starts;
        private readonly HashSet<int> _beatFrames;
        private readonly HashSet<int> _downbeatFrames;
        private int _callCount;

        public MockBeatModelRuntime(
            IReadOnlyList<int> starts,
            IEnumerable<int> beatFrames,
            IEnumerable<int> downbeatFrames)
        {
            _starts = starts;
            _beatFrames = new HashSet<int>(beatFrames);
            _downbeatFrames = new HashSet<int>(downbeatFrames);
        }

        public string ModelPath => "mock.onnx";
        public IReadOnlyList<string> InputNames => ["input"];
        public IReadOnlyList<string> OutputNames => ["beat", "downbeat"];

        public BeatThisInferenceResult Run(BeatThisInputTensor inputTensor, BeatThisModelMetadata metadata)
        {
            var chunkFrames = inputTensor.ChunkFrames;
            var beatActivations = new float[chunkFrames];
            var downbeatActivations = new float[chunkFrames];

            Array.Fill(beatActivations, -10.0f);
            Array.Fill(downbeatActivations, -10.0f);

            if (_callCount < _starts.Count)
            {
                var start = _starts[_callCount++];
                for (var localFrame = 0; localFrame < chunkFrames; localFrame++)
                {
                    var globalFrame = start + localFrame;
                    if (_beatFrames.Contains(globalFrame))
                    {
                        beatActivations[localFrame] = 10.0f;
                    }
                    if (_downbeatFrames.Contains(globalFrame))
                    {
                        downbeatActivations[localFrame] = 10.0f;
                    }
                }
            }

            return new BeatThisInferenceResult
            {
                BeatActivations = beatActivations,
                DownbeatActivations = downbeatActivations,
                FrameRate = inputTensor.FrameRate,
                ValidFrameCount = chunkFrames
            };
        }

        public void Dispose() { }
    }

    private static FeatureMatrix CreateDummyFeatures() => new()
    {
        Mfcc = [],
        Chroma = [],
        SpectralFlux = [],
        Rms = [],
        FrameSizeSamples = 2048,
        HopLengthSamples = 512,
        SampleRate = 22050
    };

    [Fact]
    public void BeatThisProvider_does_not_fallback_when_only_downbeats_are_bad()
    {
        using var tempDir = TemporaryDirectory.Create();
        var modelPath = Path.Combine(tempDir.Path, BeatThisModelLocator.DefaultModelFileName);
        File.WriteAllBytes(modelPath, [1, 2, 3, 4]);

        var metadataPath = Path.Combine(tempDir.Path, "model.json");
        File.WriteAllText(metadataPath, @"{
            ""name"": ""beat-this"",
            ""version"": ""1.0.0"",
            ""license"": ""MIT"",
            ""sample_rate"": 44100,
            ""frame_rate"": 100.0,
            ""frame_size"": 2048,
            ""hop_size"": 1024
        }");

        var modelLocator = new BeatThisModelLocator(tempDir.Path);
        var aggregateRunner = new BeatThisOfficialAggregateRunner();
        var advisorPostprocessor = CreateStrictPeakPostprocessor();
        var guardrails = new BeatGridGuardrails(new BeatGridGuardrailOptions
        {
            MaxDownbeatToBeatDistanceSeconds = 0.03
        });

        var beatFrames = new[] { 100, 200, 300, 400 };
        var downbeatFrames = new[] { 100, 290 };

        var frameCount = 500;
        var starts = aggregateRunner.BuildStarts(frameCount);

        var runtimeFactory = new Func<string, IBeatModelRuntime>(_ =>
            new MockBeatModelRuntime(starts, beatFrames, downbeatFrames));

        var beatThisTracker = new BeatThisOnnxBeatTracker(
            modelLocator,
            runtimeFactory,
            postprocessor: null,
            aggregateRunner: aggregateRunner,
            advisorPostprocessor: advisorPostprocessor,
            guardrailOptions: new BeatGridGuardrailOptions
            {
                MaxDownbeatToBeatDistanceSeconds = 0.03
            });

        var builtInTracker = new SpectralFluxBeatTracker();
        var selector = new BeatTrackerSelector(
            builtInTracker,
            beatThisTracker,
            guardrails);

        var audio = new LoadedAudio(new float[44100 * 5], 44100, 5.0, "hash", "song.wav", "song.wav");
        var features = CreateDummyFeatures();
        var options = new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.BeatThis,
            AiFallbackMode = AiFallbackMode.FallbackToBuiltIn
        };

        var result = selector.Track(audio, features, options);

        result.UsedAiProvider.Should().BeTrue();
        result.UsedFallbackProvider.Should().BeFalse();
        result.DownbeatSanitized.Should().BeTrue();
        result.DownbeatTimes.Should().BeEmpty();
        result.ProviderWarnings.Should().ContainSingle()
            .Which.Should().StartWith("beat-this-warning:downbeats-discarded:not-aligned-to-beat");
    }

    [Fact]
    public void BeatThisProvider_still_fallbacks_when_beats_are_bad()
    {
        using var tempDir = TemporaryDirectory.Create();
        var modelPath = Path.Combine(tempDir.Path, BeatThisModelLocator.DefaultModelFileName);
        File.WriteAllBytes(modelPath, [1, 2, 3, 4]);

        var metadataPath = Path.Combine(tempDir.Path, "model.json");
        File.WriteAllText(metadataPath, @"{
            ""name"": ""beat-this"",
            ""version"": ""1.0.0"",
            ""license"": ""MIT"",
            ""sample_rate"": 44100,
            ""frame_rate"": 100.0,
            ""frame_size"": 2048,
            ""hop_size"": 1024
        }");

        var modelLocator = new BeatThisModelLocator(tempDir.Path);
        var aggregateRunner = new BeatThisOfficialAggregateRunner();
        var advisorPostprocessor = CreateStrictPeakPostprocessor();
        var guardrails = new BeatGridGuardrails(new BeatGridGuardrailOptions
        {
            MinBeatCount = 3
        });

        var beatFrames = new[] { 100, 200 };
        var downbeatFrames = new[] { 100 };

        var frameCount = 500;
        var starts = aggregateRunner.BuildStarts(frameCount);

        var runtimeFactory = new Func<string, IBeatModelRuntime>(_ =>
            new MockBeatModelRuntime(starts, beatFrames, downbeatFrames));

        var beatThisTracker = new BeatThisOnnxBeatTracker(
            modelLocator,
            runtimeFactory,
            postprocessor: null,
            aggregateRunner: aggregateRunner,
            advisorPostprocessor: advisorPostprocessor);

        var builtInTracker = new SpectralFluxBeatTracker();
        var selector = new BeatTrackerSelector(
            builtInTracker,
            beatThisTracker,
            guardrails);

        var audio = new LoadedAudio(new float[44100 * 5], 44100, 5.0, "hash", "song.wav", "song.wav");
        var features = CreateDummyFeatures();
        var options = new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.BeatThis,
            AiFallbackMode = AiFallbackMode.FallbackToBuiltIn
        };

        var result = selector.Track(audio, features, options);

        result.UsedAiProvider.Should().BeFalse();
        result.UsedFallbackProvider.Should().BeTrue();
        result.FallbackReason.Should().StartWith("beat-this-guardrail-rejected:beat-count-too-low");
    }

    private static BeatThisAdvisorPostprocessor CreateStrictPeakPostprocessor()
    {
        return new BeatThisAdvisorPostprocessor(new BeatThisAdvisorPostprocessOptions
        {
            BeatThresholdPercentile = 99.9,
            DownbeatThresholdPercentile = 99.9
        });
    }
}
