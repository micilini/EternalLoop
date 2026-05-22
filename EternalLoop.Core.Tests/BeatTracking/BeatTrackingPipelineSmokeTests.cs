using EternalLoop.Contracts.Options;
using EternalLoop.Core.Analysis;
using EternalLoop.Core.BeatTracking;
using EternalLoop.Core.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.Core.Tests.BeatTracking;

public sealed class BeatTrackingPipelineSmokeTests
{
    [Fact]
    public void FeatureExtraction_ThenBeatTracking_Should_DetectClickTrackTempo()
    {
        var audio = TestSignalFactory.CreateClickTrackLoadedAudio(
            durationSeconds: 10.0,
            bpm: 120.0);

        var extractor = new NWavesFeatureExtractor();
        var features = extractor.Extract(audio, new FeatureExtractionOptions());

        var tracker = new SpectralFluxBeatTracker();
        var result = tracker.Track(audio, features, new BeatTrackingOptions());

        result.EstimatedBpm.Should().BeApproximately(120.0, 6.0);
        result.BeatTimes.Should().NotBeEmpty();
        result.Confidences.Should().HaveCount(result.BeatTimes.Length);
    }
}
