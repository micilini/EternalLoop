using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.AI;
using EternalLoop.Core.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.Core.Tests.AI;

public sealed class AiEmbeddingExtractorTests
{
    private const double TestDurationSeconds = 1.0;
    private const double TestFrequencyHertz = 440.0;
    private const double TestAmplitude = 0.5;
    private const float FirstEmbeddingValue = 1.0f;
    private const float SecondEmbeddingValue = 2.0f;
    private const int FirstEmbeddingIndex = 0;
    private const int SecondEmbeddingIndex = 1;
    private const int WrongOutputCountDelta = 1;
    private const int WrongEmbeddingDimensionDelta = 1;
    private const double NormTolerance = 0.0001;

    [Fact]
    public async Task ExtractAsync_returns_embeddings_for_short_audio()
    {
        var model = new FakeEmbeddingModel();
        var extractor = CreateExtractor(model);

        var result = await extractor.ExtractAsync(CreateAudio(), new ProgressRecorder(), CancellationToken.None);

        result.Frames.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_returns_normalized_vectors()
    {
        var model = new FakeEmbeddingModel();
        var extractor = CreateExtractor(model);

        var result = await extractor.ExtractAsync(CreateAudio(), new ProgressRecorder(), CancellationToken.None);

        foreach (var frame in result.Frames)
        {
            VectorNorm(frame.Vector).Should().BeApproximately(1.0, NormTolerance);
        }
    }

    [Fact]
    public async Task ExtractAsync_reports_progress()
    {
        var progress = new ProgressRecorder();
        var extractor = CreateExtractor(new FakeEmbeddingModel());

        _ = await extractor.ExtractAsync(CreateAudio(), progress, CancellationToken.None);

        progress.Entries.Should().Contain(entry => entry.Stage == AnalysisStage.RunningAi);
        progress.Entries.Select(entry => entry.Message).Should().Contain("AI embeddings ready");
    }

    [Fact]
    public async Task ExtractAsync_supports_cancellation()
    {
        var extractor = CreateExtractor(new FakeEmbeddingModel());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await extractor.ExtractAsync(CreateAudio(), new ProgressRecorder(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExtractAsync_does_not_return_nan()
    {
        var model = new FakeEmbeddingModel
        {
            IncludeNonFiniteValues = true
        };
        var extractor = CreateExtractor(model);

        var result = await extractor.ExtractAsync(CreateAudio(), new ProgressRecorder(), CancellationToken.None);

        result.Frames.SelectMany(frame => frame.Vector).Should().OnlyContain(value => float.IsFinite(value));
    }

    [Fact]
    public async Task ExtractAsync_uses_model_id_from_manifest()
    {
        var extractor = CreateExtractor(new FakeEmbeddingModel());

        var result = await extractor.ExtractAsync(CreateAudio(), new ProgressRecorder(), CancellationToken.None);

        result.ModelId.Should().Be(AiModelDefaultValues.DiscogsEffNetModelId);
    }

    [Fact]
    public async Task ExtractAsync_uses_512_embedding_dimensions()
    {
        var extractor = CreateExtractor(new FakeEmbeddingModel());

        var result = await extractor.ExtractAsync(CreateAudio(), new ProgressRecorder(), CancellationToken.None);

        result.EmbeddingDimensions.Should().Be(AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions);
        result.Frames.Should().OnlyContain(frame => frame.Vector.Length == AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions);
    }

    [Fact]
    public async Task ExtractAsync_returns_empty_result_for_empty_audio()
    {
        var extractor = CreateExtractor(new FakeEmbeddingModel());
        var audio = new LoadedAudio([], AiPreprocessingDefaultValues.SampleRate, 0.0, "empty");

        var result = await extractor.ExtractAsync(audio, new ProgressRecorder(), CancellationToken.None);

        result.Frames.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_does_not_call_model_for_empty_audio()
    {
        var model = new FakeEmbeddingModel();
        var extractor = CreateExtractor(model);
        var audio = new LoadedAudio([], AiPreprocessingDefaultValues.SampleRate, 0.0, "empty");

        _ = await extractor.ExtractAsync(audio, new ProgressRecorder(), CancellationToken.None);

        model.PredictCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ExtractAsync_rejects_wrong_model_output_count()
    {
        var model = new FakeEmbeddingModel
        {
            OutputCountDelta = WrongOutputCountDelta
        };
        var extractor = CreateExtractor(model);

        var act = async () => await extractor.ExtractAsync(CreateAudio(), new ProgressRecorder(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExtractAsync_rejects_wrong_embedding_dimension()
    {
        var model = new FakeEmbeddingModel
        {
            EmbeddingDimensions = AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions - WrongEmbeddingDimensionDelta
        };
        var extractor = CreateExtractor(model);

        var act = async () => await extractor.ExtractAsync(CreateAudio(), new ProgressRecorder(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExtractAsync_processes_more_than_one_batch()
    {
        var model = new FakeEmbeddingModel();
        var extractor = CreateExtractor(model);

        var result = await extractor.ExtractAsync(CreateMultiBatchAudio(), new ProgressRecorder(), CancellationToken.None);

        model.PredictCallCount.Should().BeGreaterThan(1);
        result.Frames.Should().HaveCountGreaterThan(AiModelDefaultValues.DiscogsEffNetBatchSize);
        result.Frames.Select(frame => frame.Index).Should().Equal(Enumerable.Range(0, result.Frames.Count));
        result.Frames.Should().OnlyContain(frame => frame.Vector.Length == AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions);
        result.Frames.SelectMany(frame => frame.Vector).Should().OnlyContain(value => float.IsFinite(value));
    }

    private static AiEmbeddingExtractor CreateExtractor(FakeEmbeddingModel model)
    {
        return new AiEmbeddingExtractor(
            new AiAudioPreprocessor(),
            new AiMelSpectrogramExtractor(new AiMelFilterBank()),
            new AiPatchExtractor(),
            new AiPatchBatcher(),
            model,
            new FakeModelProvider());
    }

    private static LoadedAudio CreateAudio(double durationSeconds = TestDurationSeconds)
    {
        return TestSignalFactory.CreateSineLoadedAudio(durationSeconds: durationSeconds);
    }

    private static LoadedAudio CreateMultiBatchAudio()
    {
        var requiredPatchCount = AiModelDefaultValues.DiscogsEffNetBatchSize + 1;
        var requiredFrameCount = (requiredPatchCount - 1) * AiPreprocessingDefaultValues.PatchHopFrames + 1;
        var requiredSampleCount = (requiredFrameCount - 1) * AiPreprocessingDefaultValues.HopLength
            + AiPreprocessingDefaultValues.FftSize;
        var samples = Enumerable.Range(0, requiredSampleCount)
            .Select(index => (float)(Math.Sin(Math.Tau * TestFrequencyHertz * index / AiPreprocessingDefaultValues.SampleRate) * TestAmplitude))
            .ToArray();
        var durationSeconds = samples.Length / (double)AiPreprocessingDefaultValues.SampleRate;

        return new LoadedAudio(samples, AiPreprocessingDefaultValues.SampleRate, durationSeconds, "multi-batch-hash");
    }

    private static double VectorNorm(float[] vector)
    {
        return Math.Sqrt(vector.Sum(value => (double)value * value));
    }

    private sealed class FakeEmbeddingModel : ILocalMusicEmbeddingModel
    {
        public string ModelId => AiModelDefaultValues.DiscogsEffNetModelId;

        public int BatchSize => AiModelDefaultValues.DiscogsEffNetBatchSize;

        public int MelBands => AiModelDefaultValues.DiscogsEffNetMelBands;

        public int PatchFrames => AiModelDefaultValues.DiscogsEffNetPatchFrames;

        public int EmbeddingDimensions { get; init; } = AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions;

        public int OutputCountDelta { get; init; }

        public bool IncludeNonFiniteValues { get; init; }

        public int PredictCallCount { get; private set; }

        public IReadOnlyList<float[]> Predict(IReadOnlyList<float[][]> patches)
        {
            PredictCallCount++;
            var outputCount = Math.Max(0, patches.Count + OutputCountDelta);
            return Enumerable.Range(0, outputCount)
                .Select(CreateEmbedding)
                .ToArray();
        }

        public void Dispose()
        {
        }

        private float[] CreateEmbedding(int index)
        {
            var vector = new float[EmbeddingDimensions];

            if (vector.Length > FirstEmbeddingIndex)
            {
                vector[FirstEmbeddingIndex] = FirstEmbeddingValue + index;
            }

            if (vector.Length > SecondEmbeddingIndex)
            {
                vector[SecondEmbeddingIndex] = SecondEmbeddingValue + index;
            }

            if (IncludeNonFiniteValues && vector.Length > SecondEmbeddingIndex)
            {
                vector[SecondEmbeddingIndex] = float.NaN;
            }

            return vector;
        }
    }

    private sealed class FakeModelProvider : IAiModelProvider
    {
        public Task<AiModelManifest> GetManifestAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new AiModelManifest
            {
                Id = AiModelDefaultValues.DiscogsEffNetModelId,
                DisplayName = AiModelDefaultValues.DiscogsEffNetDisplayName,
                Provider = AiModelDefaultValues.DiscogsEffNetProvider,
                Version = AiModelDefaultValues.DiscogsEffNetVersion,
                OnnxFile = AiModelDefaultValues.DiscogsEffNetOnnxFile,
                MetadataFile = AiModelDefaultValues.DiscogsEffNetMetadataFile,
                LicenseNoticeFile = AiModelDefaultValues.DiscogsEffNetLicenseNoticeFile,
                InputName = AiModelDefaultValues.DiscogsEffNetInputName,
                EmbeddingOutputName = AiModelDefaultValues.DiscogsEffNetEmbeddingOutputName,
                BatchSize = AiModelDefaultValues.DiscogsEffNetBatchSize,
                MelBands = AiModelDefaultValues.DiscogsEffNetMelBands,
                PatchFrames = AiModelDefaultValues.DiscogsEffNetPatchFrames,
                EmbeddingDimensions = AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions,
                SampleRate = AiModelDefaultValues.DiscogsEffNetSampleRate,
                License = AiModelDefaultValues.DiscogsEffNetLicense,
                Source = AiModelDefaultValues.DiscogsEffNetSource
            });
        }
    }

    private sealed class ProgressRecorder : IAnalysisProgressReporter
    {
        public List<ProgressEntry> Entries { get; } = [];

        public void Report(AnalysisStage stage, double progress01, string? message = null)
        {
            Entries.Add(new ProgressEntry(stage, progress01, message));
        }
    }

    private sealed record ProgressEntry(AnalysisStage Stage, double Progress, string? Message);
}
