using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai.Advisor;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Ai.Advisor;

public sealed class BeatThisOfficialAggregateRunnerGoldenTests
{
    [Fact]
    public void OfficialAggregate_builds_expected_starts_for_gangnam()
    {
        var runner = new BeatThisOfficialAggregateRunner();

        var starts = runner.BuildStarts(10_986);

        starts.Should().Equal(-6, 1482, 2970, 4458, 5946, 7434, 8922, 9492);
    }

    [Fact]
    public void OfficialAggregate_matches_expected_beat_logits_for_all_tracks()
    {
        foreach (var fixture in LoadFixtures())
        {
            var output = RunAggregate(fixture);
            var metrics = Compare(output.BeatLogits, fixture.ExpectedBeatLogits);

            metrics.Mae.Should().BeLessThanOrEqualTo(1e-3, fixture.TrackId);
            metrics.Rmse.Should().BeLessThanOrEqualTo(1e-3, fixture.TrackId);
            metrics.Pearson.Should().BeGreaterThanOrEqualTo(0.999, fixture.TrackId);
            TopKOverlapWithinFrames(output.BeatLogits, fixture.ExpectedBeatLogits, 100, 2)
                .Should().BeGreaterThanOrEqualTo(0.95, fixture.TrackId);
        }
    }

    [Fact]
    public void OfficialAggregate_matches_expected_downbeat_logits_for_all_tracks()
    {
        foreach (var fixture in LoadFixtures())
        {
            var output = RunAggregate(fixture);
            var metrics = Compare(output.DownbeatLogits, fixture.ExpectedDownbeatLogits);

            metrics.Mae.Should().BeLessThanOrEqualTo(1e-3, fixture.TrackId);
            metrics.Rmse.Should().BeLessThanOrEqualTo(1e-3, fixture.TrackId);
            metrics.Pearson.Should().BeGreaterThanOrEqualTo(0.999, fixture.TrackId);
            TopKOverlapWithinFrames(output.DownbeatLogits, fixture.ExpectedDownbeatLogits, 100, 2)
                .Should().BeGreaterThanOrEqualTo(0.95, fixture.TrackId);
        }
    }

    [Fact]
    public void OfficialAggregate_does_not_leave_unwritten_frames()
    {
        var fixture = LoadFixtures().First();
        var output = RunAggregate(fixture);

        output.BeatLogits.Should().HaveCount(fixture.SpectrogramFrames);
        output.DownbeatLogits.Should().HaveCount(fixture.SpectrogramFrames);
        output.ChunkCount.Should().Be(8);
    }

    [Fact]
    public void OfficialAggregate_does_not_use_dry_chunk_concatenation()
    {
        var fixture = LoadFixtures().First();
        var runner = new BeatThisOfficialAggregateRunner();
        using var runtime = new LocalFrameIndexRuntime();
        var output = runner.Run(
            new BeatThisSpectrogram(
                fixture.Spectrogram,
                fixture.SpectrogramFrames,
                fixture.MelBins,
                fixture.FrameRate,
                fixture.DurationSeconds),
            runtime,
            CreateMetadata());

        output.BeatLogits[0].Should().Be(6.0f);
        output.BeatLogits[1482].Should().Be(1488.0f);
        output.BeatLogits[1488].Should().Be(6.0f);
        output.BeatLogits[^1].Should().Be(1493.0f);
    }

    public static IReadOnlyList<(string TrackId, double BeatMae, double BeatPearson, double BeatTop100, double DownbeatMae, double DownbeatPearson, double DownbeatTop100)> CalculateGoldenMetrics()
    {
        return LoadFixtures()
            .Select(fixture =>
            {
                var output = RunAggregate(fixture);
                var beat = Compare(output.BeatLogits, fixture.ExpectedBeatLogits);
                var downbeat = Compare(output.DownbeatLogits, fixture.ExpectedDownbeatLogits);

                return (
                    fixture.TrackId,
                    beat.Mae,
                    beat.Pearson,
                    TopKOverlapWithinFrames(output.BeatLogits, fixture.ExpectedBeatLogits, 100, 2),
                    downbeat.Mae,
                    downbeat.Pearson,
                    TopKOverlapWithinFrames(output.DownbeatLogits, fixture.ExpectedDownbeatLogits, 100, 2));
            })
            .ToArray();
    }

    private static BeatThisAdvisorOutput RunAggregate(AdvisorGoldenMasterFixture fixture)
    {
        var metadata = CreateMetadata();
        var spectrogram = new BeatThisSpectrogram(
            fixture.Spectrogram,
            fixture.SpectrogramFrames,
            fixture.MelBins,
            fixture.FrameRate,
            fixture.DurationSeconds);
        var runner = new BeatThisOfficialAggregateRunner();
        using var runtime = new OnnxBeatModelRuntime(FindRepoFile(Path.Combine("assets", "models", "beat-this", "beat-this-large.onnx")));

        return runner.Run(spectrogram, runtime, metadata);
    }

    private static BeatThisModelMetadata CreateMetadata()
    {
        return new BeatThisModelMetadata
        {
            Name = "beat-this-large",
            Version = "final0",
            License = "MIT",
            SampleRate = 22_050,
            FrameRate = 50.0,
            OutputNames = ["beat_logits", "downbeat_logits"],
            ChunkFrames = 1_500,
            MelBins = 128,
            FrameSize = 1_024
        };
    }

    private static IReadOnlyList<AdvisorGoldenMasterFixture> LoadFixtures()
    {
        var reader = new AdvisorGoldenMasterZipReader(GoldenZipPath());

        return reader.TrackIds.Select(reader.LoadTrack).ToArray();
    }

    private static string GoldenZipPath()
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "AdvisorGoldenMasters",
            "EternalLoop.AdvisorGoldenMasters.v1.zip");
    }

    private static string FindRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file: {relativePath}");
    }

    private static (double Mae, double Rmse, double Pearson) Compare(float[] actual, float[] expected)
    {
        actual.Should().HaveCount(expected.Length);

        var absolute = 0.0;
        var squared = 0.0;
        var actualMean = actual.Average(value => (double)value);
        var expectedMean = expected.Average(value => (double)value);
        var covariance = 0.0;
        var actualVariance = 0.0;
        var expectedVariance = 0.0;

        for (var index = 0; index < actual.Length; index++)
        {
            var delta = actual[index] - expected[index];
            absolute += Math.Abs(delta);
            squared += delta * delta;
            var actualCentered = actual[index] - actualMean;
            var expectedCentered = expected[index] - expectedMean;
            covariance += actualCentered * expectedCentered;
            actualVariance += actualCentered * actualCentered;
            expectedVariance += expectedCentered * expectedCentered;
        }

        return (
            absolute / actual.Length,
            Math.Sqrt(squared / actual.Length),
            covariance / Math.Sqrt(actualVariance * expectedVariance));
    }

    private static double TopKOverlapWithinFrames(float[] actual, float[] expected, int k, int toleranceFrames)
    {
        var actualTop = TopKIndexes(actual, k);
        var expectedTop = TopKIndexes(expected, k);
        var matches = actualTop.Count(actualIndex =>
            expectedTop.Any(expectedIndex => Math.Abs(actualIndex - expectedIndex) <= toleranceFrames));

        return matches / (double)k;
    }

    private static int[] TopKIndexes(float[] values, int k)
    {
        return values
            .Select((value, index) => (value, index))
            .OrderByDescending(item => item.value)
            .Take(k)
            .Select(item => item.index)
            .ToArray();
    }

    private sealed class LocalFrameIndexRuntime : IBeatModelRuntime
    {
        public string ModelPath => "fake";

        public IReadOnlyList<string> InputNames => ["spectrogram"];

        public IReadOnlyList<string> OutputNames => ["beat_logits", "downbeat_logits"];

        public BeatThisInferenceResult Run(BeatThisInputTensor inputTensor, BeatThisModelMetadata metadata)
        {
            var values = Enumerable.Range(0, inputTensor.ChunkFrames)
                .Select(index => (float)index)
                .ToArray();

            return new BeatThisInferenceResult
            {
                BeatActivations = values,
                DownbeatActivations = values.ToArray(),
                FrameRate = inputTensor.FrameRate,
                ValidFrameCount = inputTensor.ChunkFrames,
                AudioDurationSeconds = inputTensor.DurationSeconds,
                OutputMode = "fake"
            };
        }

        public void Dispose()
        {
        }
    }
}
