using System.Text.Json;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai.Advisor;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Ai.Advisor;

public sealed class BeatThisAdvisorPostprocessorGoldenTests
{
    [Fact]
    public void Postprocessor_reproduces_expected_beat_events_for_all_tracks()
    {
        foreach (var fixture in LoadFixtures())
        {
            var result = Postprocess(fixture);
            var expected = ReadEvents(fixture.ExpectedPostprocessBeats);
            var f1_70 = CalculateF1(expected, result.BeatTimes, 0.070);
            var f1_100 = CalculateF1(expected, result.BeatTimes, 0.100);
            var countRatio = result.BeatTimes.Length / (double)expected.Length;
            var bpmDelta = Math.Abs(EstimateBpm(result.BeatTimes) - EstimateBpm(expected));

            countRatio.Should().BeLessThanOrEqualTo(1.05, fixture.TrackId);
            f1_70.Should().BeGreaterThanOrEqualTo(0.98, fixture.TrackId);
            f1_100.Should().BeGreaterThanOrEqualTo(0.98, fixture.TrackId);
            bpmDelta.Should().BeLessThanOrEqualTo(0.5, fixture.TrackId);
        }
    }

    [Fact]
    public void Postprocessor_does_not_create_dense_grid_for_all_tracks()
    {
        foreach (var fixture in LoadFixtures())
        {
            var result = Postprocess(fixture);

            result.IsDenseGrid.Should().BeFalse(fixture.TrackId);
            result.RejectionReason.Should().BeNull(fixture.TrackId);
        }
    }

    [Fact]
    public void Postprocessor_uses_raw_logits_without_minmax()
    {
        var output = new BeatThisAdvisorOutput
        {
            BeatLogits = [0.0f, 100.0f, 0.0f, 20.0f, 0.0f, 10.0f, 0.0f, 1.0f],
            DownbeatLogits = [0.0f, 50.0f, 0.0f, 1.0f, 0.0f, 0.5f, 0.0f, 0.25f],
            FrameCount = 8,
            FrameRate = 4.0,
            DurationSeconds = 2.0,
            ChunkCount = 1,
            OutputMode = "test",
            AggregatePolicy = "keep_first"
        };
        var postprocessor = new BeatThisAdvisorPostprocessor(new BeatThisAdvisorPostprocessOptions
        {
            BeatThresholdPercentile = 0.5,
            DownbeatThresholdPercentile = 0.5,
            MinBeatSpacingSeconds = 0.25,
            MinDownbeatSpacingSeconds = 0.25,
            MinBpm = 1.0,
            MaxBpm = 1_000.0,
            MinMedianIntervalSeconds = 0.01
        });

        var result = postprocessor.Postprocess(output);

        result.Transform.Should().Be("raw");
        result.BeatTimes.First().Should().Be(0.25);
    }

    [Fact]
    public void Postprocessor_respects_track_specific_best_contract()
    {
        foreach (var fixture in LoadFixtures())
        {
            var best = fixture.PostprocessContract.RootElement.GetProperty("track_specific_best");

            best.GetProperty("transform").GetString().Should().Be("raw");
            best.GetProperty("local_maxima_window").GetInt32().Should().Be(1);
            best.GetProperty("min_spacing_seconds").GetDouble().Should().Be(0.25);

            var result = Postprocess(fixture);

            result.Algorithm.Should().Be("local_maxima_percentile_min_spacing");
            result.Transform.Should().Be("raw");
        }
    }

    [Fact]
    public void Expected_postprocess_downbeats_load_and_result_is_not_dense()
    {
        foreach (var fixture in LoadFixtures())
        {
            var expectedDownbeats = ReadEvents(fixture.ExpectedPostprocessDownbeats);
            var result = Postprocess(fixture);

            expectedDownbeats.Should().NotBeEmpty(fixture.TrackId);
            result.DownbeatTimes.Should().NotBeEmpty(fixture.TrackId);
            result.IsDenseGrid.Should().BeFalse(fixture.TrackId);
        }
    }

    public static IReadOnlyList<(string TrackId, double F1_70, double F1_100, double CountRatio, double Bpm)> CalculatePostprocessMetrics()
    {
        return LoadFixtures()
            .Select(fixture =>
            {
                var result = Postprocess(fixture);
                var expected = ReadEvents(fixture.ExpectedPostprocessBeats);

                return (
                    fixture.TrackId,
                    CalculateF1(expected, result.BeatTimes, 0.070),
                    CalculateF1(expected, result.BeatTimes, 0.100),
                    result.BeatTimes.Length / (double)expected.Length,
                    result.EstimatedBpm);
            })
            .ToArray();
    }

    private static BeatThisAdvisorPostprocessResult Postprocess(AdvisorGoldenMasterFixture fixture)
    {
        var best = fixture.PostprocessContract.RootElement.GetProperty("track_specific_best");
        var output = new BeatThisAdvisorOutput
        {
            BeatLogits = fixture.ExpectedBeatLogits,
            DownbeatLogits = fixture.ExpectedDownbeatLogits,
            FrameCount = fixture.SpectrogramFrames,
            FrameRate = fixture.FrameRate,
            DurationSeconds = fixture.DurationSeconds,
            ChunkCount = 1,
            OutputMode = "golden",
            AggregatePolicy = "keep_first"
        };
        var postprocessor = new BeatThisAdvisorPostprocessor(new BeatThisAdvisorPostprocessOptions
        {
            LocalMaximaWindowFrames = best.GetProperty("local_maxima_window").GetInt32(),
            BeatThresholdPercentile = best.GetProperty("threshold_percentile").GetDouble(),
            DownbeatThresholdPercentile = fixture.PostprocessContract.RootElement
                .GetProperty("track_specific_downbeat_best")
                .GetProperty("threshold_percentile")
                .GetDouble(),
            MinBeatSpacingSeconds = best.GetProperty("min_spacing_seconds").GetDouble(),
            MinDownbeatSpacingSeconds = fixture.PostprocessContract.RootElement
                .GetProperty("track_specific_downbeat_best")
                .GetProperty("min_spacing_seconds")
                .GetDouble()
        });

        return postprocessor.Postprocess(output);
    }

    private static double[] ReadEvents(JsonDocument document)
    {
        return document.RootElement
            .GetProperty("events_seconds")
            .EnumerateArray()
            .Select(value => value.GetDouble())
            .ToArray();
    }

    private static double CalculateF1(double[] reference, double[] candidate, double toleranceSeconds)
    {
        var used = new bool[reference.Length];
        var matches = 0;

        foreach (var candidateTime in candidate)
        {
            var bestIndex = -1;
            var bestDistance = double.PositiveInfinity;

            for (var index = 0; index < reference.Length; index++)
            {
                if (used[index])
                {
                    continue;
                }

                var distance = Math.Abs(reference[index] - candidateTime);

                if (distance <= toleranceSeconds && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = index;
                }
            }

            if (bestIndex >= 0)
            {
                used[bestIndex] = true;
                matches++;
            }
        }

        var precision = candidate.Length == 0 ? 0.0 : matches / (double)candidate.Length;
        var recall = reference.Length == 0 ? 0.0 : matches / (double)reference.Length;

        return precision + recall > 0.0
            ? 2.0 * precision * recall / (precision + recall)
            : 0.0;
    }

    private static double EstimateBpm(double[] beatTimes)
    {
        var intervals = beatTimes
            .Zip(beatTimes.Skip(1), (left, right) => right - left)
            .Where(interval => interval > 0.0)
            .Order()
            .ToArray();

        if (intervals.Length == 0)
        {
            return 0.0;
        }

        return 60.0 / intervals[intervals.Length / 2];
    }

    private static IReadOnlyList<AdvisorGoldenMasterFixture> LoadFixtures()
    {
        var reader = new AdvisorGoldenMasterZipReader(Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "AdvisorGoldenMasters",
            "EternalLoop.AdvisorGoldenMasters.v1.zip"));

        return reader.TrackIds.Select(reader.LoadTrack).ToArray();
    }
}
