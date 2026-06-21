using System.Text.Json;
using EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;
using EternalLoop.AnalysisEngine.Core.Export;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.WeakWindows;

public sealed class BeatGridWeakWindowSerializationTests
{
    [Fact]
    public void Diagnostics_serializes_enums_as_strings()
    {
        var json = JsonSerializer.Serialize(CreateDiagnostics(), JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"risk_level\": \"Low\"");
        json.Should().Contain("\"correction_readiness\": \"CandidateForReview\"");
    }

    [Fact]
    public void Diagnostics_serializes_windows()
    {
        JsonSerializer.Serialize(CreateDiagnostics(), JsonWriterOptionsFactory.Create(pretty: true))
            .Should().Contain("\"windows\"");
    }

    [Fact]
    public void Diagnostics_serializes_safety_flags_false()
    {
        var json = JsonSerializer.Serialize(CreateDiagnostics(), JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"should_modify_final_grid\": false");
        json.Should().Contain("\"should_select_advisor\": false");
        json.Should().Contain("\"should_apply_correction\": false");
    }

    [Fact]
    public void Diagnostics_serializes_madmom_claim_status()
    {
        JsonSerializer.Serialize(CreateDiagnostics(), JsonWriterOptionsFactory.Create(pretty: true))
            .Should().Contain("\"external_benchmark_claim_status\": \"not-evaluated\"");
    }

    private static BeatGridWeakWindowDiagnostics CreateDiagnostics()
    {
        return new BeatGridWeakWindowDiagnostics
        {
            Enabled = true,
            Status = "evaluated",
            ExternalBenchmarkClaimStatus = "not-evaluated",
            Windows =
            [
                new BeatGridWeakWindow
                {
                    Index = 0,
                    RiskLevel = BeatGridWeakWindowRiskLevel.Low,
                    CorrectionReadiness = BeatGridWeakWindowCorrectionReadiness.CandidateForReview,
                    AdvisorStrength = BeatGridWeakWindowCandidateStrength.Strong,
                    Metrics = new BeatGridWeakWindowLocalMetrics(),
                    Reasons = [BeatGridWeakWindowReason.LegacyTempoInstability]
                }
            ]
        };
    }
}
