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
            MicroFingerprints =
            [
                new BeatMicroFingerprint
                {
                    BeatIndex = 0,
                    Microsegments =
                    [
                        new BeatMicrosegment
                        {
                            BeatIndex = 0,
                            SegmentIndex = 0,
                            Start = 0.0,
                            Duration = 0.125,
                            RelativePosition = 0f,
                            Timbre = [1f],
                            Pitches = [1f],
                            Loudness = [0f, 0f, 0f],
                            Flux = 0.1f
                        }
                    ]
                }
            ],
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
        json.Should().Contain(nameof(TrackAnalysis.MicroFingerprints));
    }

    [Fact]
    public void TrackAnalysis_schema_version_is_1_2_0()
    {
        TrackAnalysis.CurrentSchemaVersion.Should().Be(ProductInfo.Version);
        TrackAnalysis.CurrentSchemaVersion.Should().Be("1.2.0");
    }

    [Fact]
    public void TrackAnalysis_Should_SerializeAndDeserializeMicroFingerprints()
    {
        var analysis = CreateAnalysisWithMicroFingerprints();

        var json = JsonSerializer.Serialize(analysis);
        var roundtrip = JsonSerializer.Deserialize<TrackAnalysis>(json);

        roundtrip.Should().NotBeNull();
        roundtrip!.MicroFingerprints.Should().HaveCount(1);
        roundtrip.MicroFingerprints[0].Microsegments.Should().HaveCount(1);
        roundtrip.MicroFingerprints[0].Microsegments[0].Flux.Should().Be(0.1f);
    }

    [Fact]
    public void TrackAnalysis_Should_DefaultMicroFingerprintsToEmptyList()
    {
        var json = """
            {
              "Metadata": {
                "FileHash": "hash",
                "FilePath": "track.wav",
                "DurationSeconds": 1.0,
                "SampleRate": 22050,
                "Tempo": 120,
                "TimeSignature": 4,
                "SchemaVersion": "1.2.0"
              },
              "Segments": [],
              "Beats": [],
              "Bars": [],
              "Tatums": [],
              "Sections": []
            }
            """;

        var analysis = JsonSerializer.Deserialize<TrackAnalysis>(json);

        analysis.Should().NotBeNull();
        analysis!.MicroFingerprints.Should().NotBeNull();
        analysis.MicroFingerprints.Should().BeEmpty();
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

    private static TrackAnalysis CreateAnalysisWithMicroFingerprints()
    {
        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = "abc123",
                FilePath = "track.wav",
                DurationSeconds = 1.0,
                SampleRate = 22_050,
                Tempo = 120,
                TimeSignature = 4,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion
            },
            Segments = [],
            Beats = [],
            Bars = [],
            Tatums = [],
            Sections = [],
            MicroFingerprints =
            [
                new BeatMicroFingerprint
                {
                    BeatIndex = 0,
                    Microsegments =
                    [
                        new BeatMicrosegment
                        {
                            BeatIndex = 0,
                            SegmentIndex = 0,
                            Start = 0.0,
                            Duration = 0.125,
                            RelativePosition = 0f,
                            Timbre = [1f],
                            Pitches = [1f],
                            Loudness = [0f, 0f, 0f],
                            Flux = 0.1f
                        }
                    ]
                }
            ]
        };
    }
}
