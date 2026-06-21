using System.Text.Json;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Agreement;
using EternalLoop.AnalysisEngine.Core.Export;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Agreement;

public sealed class BeatGridAgreementConfidenceSerializationTests
{
    [Fact]
    public void Diagnostics_serializes_levels_as_strings()
    {
        var json = JsonSerializer.Serialize(CreateDiagnostics(), JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"level\": \"High\"");
    }

    [Fact]
    public void Diagnostics_serializes_windows()
    {
        var json = JsonSerializer.Serialize(CreateDiagnostics(), JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"windows\"");
        json.Should().Contain("\"future_fusion_candidate\": true");
    }

    [Fact]
    public void Diagnostics_serializes_madmom_claim_status()
    {
        var json = JsonSerializer.Serialize(CreateDiagnostics(), JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"external_benchmark_claim_status\": \"not-evaluated\"");
    }

    [Fact]
    public void Diagnostics_serializes_safety_flags_false()
    {
        var json = JsonSerializer.Serialize(CreateDiagnostics(), JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"should_modify_final_grid\": false");
        json.Should().Contain("\"should_select_advisor\": false");
        json.Should().Contain("\"should_apply_correction\": false");
    }

    private static BeatGridAgreementConfidenceDiagnostics CreateDiagnostics()
    {
        return new BeatGridAgreementConfidenceDiagnostics
        {
            Enabled = true,
            Status = "evaluated",
            GlobalConfidence = new BeatGridAgreementConfidenceScore
            {
                Level = BeatGridAgreementConfidenceLevel.High,
                Score = 0.85,
                F1_70Ms = 0.90,
                IsReliable = true
            },
            ExternalBenchmarkClaimStatus = "not-evaluated",
            Windows =
            [
                new BeatGridAgreementConfidenceWindow
                {
                    Index = 0,
                    Confidence = new BeatGridAgreementConfidenceScore
                    {
                        Level = BeatGridAgreementConfidenceLevel.High,
                        Score = 0.86,
                        F1_70Ms = 0.90,
                        IsReliable = true
                    },
                    FutureFusionCandidate = true
                }
            ]
        };
    }
}
