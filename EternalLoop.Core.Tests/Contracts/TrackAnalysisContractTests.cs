using EternalLoop.Contracts;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using FluentAssertions;
using System.Text.Json;

namespace EternalLoop.Core.Tests.Contracts;

public sealed class TrackAnalysisContractTests
{
    [Fact]
    public void TrackAnalysis_Should_BeSerializableToJson()
    {
        var analysis = new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = "abc123",
                FilePath = @"C:\Music\test.wav",
                DurationSeconds = 120.5,
                SampleRate = 22050,
                Tempo = 128,
                TimeSignature = 4,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion
            },
            Segments = Array.Empty<Segment>(),
            Beats = new[]
            {
                new Beat
                {
                    Index = 0,
                    Start = 0,
                    Duration = 0.5,
                    Confidence = 0.9,
                    Timbre = new float[] { 0.1f, 0.2f },
                    Pitches = new float[] { 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f },
                    Loudness = [0f, 0f, 0f],
                    BarPosition = [0f, 1f]
                }
            },
            Bars = Array.Empty<Bar>(),
            Tatums = Array.Empty<Tatum>(),
            Sections = Array.Empty<Section>(),
            Ai = new AiAnalysisData
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
                        Vector = [1f, 0f, 0f]
                    }
                ]
            }
        };

        var json = JsonSerializer.Serialize(analysis);

        json.Should().Contain("abc123");
        json.Should().Contain("120.5");
        json.Should().Contain(TrackAnalysis.CurrentSchemaVersion);
        json.Should().Contain(AiModelDefaultValues.DiscogsEffNetModelId);
        json.Should().Contain(nameof(TrackAnalysis.Ai));
    }

    [Fact]
    public void TrackAnalysis_Should_Expose_CurrentSchemaVersion()
    {
        TrackAnalysis.CurrentSchemaVersion.Should().Be(ProductInfo.Version);
        TrackAnalysis.CurrentSchemaVersion.Should().Be("1.1.0");
    }

    [Fact]
    public void Beat_Should_RequireLoudnessVector()
    {
        var beat = new Beat
        {
            Index = 0,
            Start = 0,
            Duration = 0.5,
            Confidence = 1.0,
            Timbre = [],
            Pitches = [],
            Loudness = [0f, 0f, 0f],
            BarPosition = [0f, 1f]
        };

        beat.Loudness.Should().NotBeNull();
        beat.Loudness.Length.Should().Be(3);
    }

    [Fact]
    public void Beat_Should_RequireBarPositionVector()
    {
        var beat = new Beat
        {
            Index = 0,
            Start = 0,
            Duration = 0.5,
            Confidence = 1.0,
            Timbre = [],
            Pitches = [],
            Loudness = [0f, 0f, 0f],
            BarPosition = [0f, 1f]
        };

        beat.BarPosition.Should().NotBeNull();
        beat.BarPosition.Length.Should().Be(2);
    }
}
