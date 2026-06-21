using System.Text.Json;
using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using EternalLoop.AnalysisEngine.Core.Export;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Candidates;

public sealed class BeatGridCandidateSerializationTests
{
    [Fact]
    public void Candidate_serializes_source_and_role_as_strings()
    {
        var candidate = new BeatGridCandidateFactory().FromResult(
            CreateResult(),
            BeatGridCandidateSourceKind.LegacyBuiltIn,
            BeatGridCandidateRole.SafeAuthority,
            "legacy");

        var json = JsonSerializer.Serialize(candidate, JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"source\": \"LegacyBuiltIn\"");
        json.Should().Contain("\"role\": \"SafeAuthority\"");
    }

    [Fact]
    public void CandidateSet_serializes_diagnostics()
    {
        var set = new BeatGridCandidateFactory().CreateShadowSet(CreateResult(), CreateResult("beat-this"), advisorAvailable: true);

        var json = JsonSerializer.Serialize(set, JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"diagnostics\"");
        json.Should().Contain("\"selected_candidate_id\": \"legacy\"");
        json.Should().Contain("\"advisor_candidate_id\": \"beat-this-advisor\"");
    }

    [Fact]
    public void CandidateSet_serializes_quality_without_cycles()
    {
        var set = new BeatGridCandidateFactory().CreateShadowSet(CreateResult(), CreateResult("beat-this"), advisorAvailable: true);

        var json = JsonSerializer.Serialize(set, JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"quality\"");
        json.Should().Contain("\"beat_count\": 4");
        json.Should().NotContain("$id");
    }

    [Fact]
    public void CandidateSet_serializes_phase_alignment()
    {
        var set = new BeatGridCandidateFactory().CreateShadowSet(
            CreateResult(),
            CreateResult("beat-this"),
            advisorAvailable: true,
            phaseAlignment: new BeatGridPhaseAlignmentDiagnostics
            {
                Enabled = true,
                Status = "offset-detected",
                BestOffsetMs = -40.0,
                ShouldApplyCorrection = false
            });

        var json = JsonSerializer.Serialize(set, JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"phase_alignment\"");
        json.Should().Contain("\"best_offset_ms\": -40");
        json.Should().Contain("\"should_apply_correction\": false");
    }

    private static BeatTrackingResult CreateResult(string provider = "built-in")
    {
        return new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [0.9, 0.9, 0.9, 0.9],
            ProviderName = provider
        };
    }
}
