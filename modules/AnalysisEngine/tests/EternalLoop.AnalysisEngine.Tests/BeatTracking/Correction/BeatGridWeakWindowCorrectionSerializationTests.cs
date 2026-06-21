using System.Text.Json;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;
using EternalLoop.AnalysisEngine.Core.Export;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Correction;

public sealed class BeatGridWeakWindowCorrectionSerializationTests
{
    [Fact]
    public void Correction_diagnostics_serializes_enums_as_strings()
    {
        var json = JsonSerializer.Serialize(CreatePlan(), JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"mode\": \"ExperimentalCandidate\"");
        json.Should().Contain("\"decision\": \"CandidateCreated\"");
    }

    [Fact]
    public void Correction_plan_serializes_windows()
    {
        JsonSerializer.Serialize(CreatePlan(), JsonWriterOptionsFactory.Create(pretty: true)).Should().Contain("\"windows\"");
    }

    [Fact]
    public void Safety_flags_serialize_false()
    {
        var json = JsonSerializer.Serialize(CreateDiagnostics(), JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"should_modify_final_grid\": false");
        json.Should().Contain("\"should_select_corrected_candidate\": false");
        json.Should().Contain("\"should_apply_correction\": false");
    }

    [Fact]
    public void Madmom_claim_status_serializes_not_evaluated()
    {
        JsonSerializer.Serialize(CreateDiagnostics(), JsonWriterOptionsFactory.Create(pretty: true))
            .Should().Contain("\"external_benchmark_claim_status\": \"not-evaluated\"");
    }

    private static BeatGridWeakWindowCorrectionPlan CreatePlan()
    {
        return new BeatGridWeakWindowCorrectionPlan
        {
            Enabled = true,
            Mode = BeatGridWeakWindowCorrectionMode.ExperimentalCandidate,
            Windows =
            [
                new BeatGridWeakWindowCorrectionWindow
                {
                    Decision = BeatGridWeakWindowCorrectionDecision.CandidateCreated,
                    Risk = BeatGridWeakWindowCorrectionRisk.Low
                }
            ]
        };
    }

    private static BeatGridWeakWindowCorrectionDiagnostics CreateDiagnostics()
    {
        return new BeatGridWeakWindowCorrectionDiagnostics
        {
            Enabled = true,
            Status = "candidate-created",
            Mode = BeatGridWeakWindowCorrectionMode.ExperimentalCandidate,
            ExternalBenchmarkClaimStatus = "not-evaluated"
        };
    }
}
