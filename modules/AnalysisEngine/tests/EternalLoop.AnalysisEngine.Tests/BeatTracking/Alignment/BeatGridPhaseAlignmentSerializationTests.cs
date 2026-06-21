using System.Text.Json;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;
using EternalLoop.AnalysisEngine.Core.Export;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Alignment;

public sealed class BeatGridPhaseAlignmentSerializationTests
{
    [Fact]
    public void Diagnostics_serializes_confidence_as_string()
    {
        var diagnostics = CreateDiagnostics();

        var json = JsonSerializer.Serialize(diagnostics, JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"confidence\": \"High\"");
    }

    [Fact]
    public void Diagnostics_serializes_windows()
    {
        var diagnostics = CreateDiagnostics();

        var json = JsonSerializer.Serialize(diagnostics, JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"windows\"");
        json.Should().Contain("\"best_offset_ms\": -40");
    }

    [Fact]
    public void Diagnostics_serializes_should_apply_correction_false()
    {
        var diagnostics = CreateDiagnostics();

        var json = JsonSerializer.Serialize(diagnostics, JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"should_apply_correction\": false");
    }

    private static BeatGridPhaseAlignmentDiagnostics CreateDiagnostics()
    {
        return new BeatGridPhaseAlignmentDiagnostics
        {
            Enabled = true,
            Status = "offset-detected",
            ReferenceCandidateId = "legacy",
            CandidateId = "beat-this-advisor",
            BestOffsetMs = -40.0,
            Confidence = BeatGridPhaseAlignmentConfidence.High,
            ShouldApplyCorrection = false,
            Windows =
            [
                new BeatGridPhaseAlignmentWindow
                {
                    Index = 0,
                    StartTimeSeconds = 0.0,
                    EndTimeSeconds = 16.0,
                    LegacyBeatCount = 32,
                    AdvisorBeatCount = 32,
                    BestOffsetMs = -40.0,
                    BestOffsetF1_70Ms = 1.0,
                    IsReliable = true
                }
            ]
        };
    }
}
