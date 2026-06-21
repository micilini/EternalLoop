using System.Text.Json;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Hybrid;
using EternalLoop.AnalysisEngine.Core.Export;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Hybrid;

public sealed class BeatGridHybridSerializationTests
{
    [Fact]
    public void HybridSelection_serializes_decision_as_string()
    {
        var diagnostics = new BeatGridHybridSelectionDiagnostics
        {
            Enabled = true,
            Decision = BeatGridHybridSelectionDecision.SelectedCorrectedExperimental
        };

        JsonSerializer.Serialize(diagnostics, JsonWriterOptionsFactory.Create(pretty: true))
            .Should().Contain("\"decision\": \"SelectedCorrectedExperimental\"");
    }

    [Fact]
    public void CandidateSet_serializes_hybrid_selection()
    {
        var set = BeatGridHybridSafetyGateTests.CreateSet();
        var (selected, diagnostics) = new BeatGridHybridSelector().SelectExplicitHybrid(set);
        set = set.WithHybridSelection(selected, diagnostics);

        JsonSerializer.Serialize(set, JsonWriterOptionsFactory.Create(pretty: true))
            .Should().Contain("\"hybrid_selection\"");
    }

    [Fact]
    public void HybridSelection_serializes_madmom_claim_status_not_evaluated()
    {
        JsonSerializer.Serialize(new BeatGridHybridSelectionDiagnostics(), JsonWriterOptionsFactory.Create(pretty: true))
            .Should().Contain("\"external_benchmark_claim_status\": \"not-evaluated\"");
    }
}
