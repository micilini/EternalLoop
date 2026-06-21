using System.Text.Json;
using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Shadow;
using EternalLoop.AnalysisEngine.Core.Export;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Models;

public sealed class TrackAnalysisBeatProviderSerializationTests
{
    [Fact]
    public void TrackAnalysis_serializes_beat_provider_but_not_internal_diagnostics()
    {
        var analysis = CreateAnalysis();

        var json = JsonSerializer.Serialize(analysis, JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"beat_provider\"");
        json.Should().Contain("\"name\": \"beat-this\"");
        json.Should().Contain("\"mode\": \"onnx-local\"");
        json.Should().Contain("\"model_name\": \"beat-this-large\"");
        json.Should().NotContain("\"Diagnostics\"");
        json.Should().NotContain("\"BeatProviderName\"");
    }

    [Fact]
    public void TrackAnalysis_serializes_shadow_under_beat_provider()
    {
        var analysis = CreateAnalysis(new BeatProviderExportDiagnostics
        {
            Name = "built-in",
            Mode = "dsp-shadow",
            UsedAi = false,
            UsedBuiltIn = true,
            UsedFallback = false,
            Shadow = BeatGridShadowDiagnostics.NotConfigured(new BeatTrackingResult
            {
                EstimatedBpm = 120.0,
                BeatTimes = [0.0, 0.5, 1.0, 1.5],
                Confidences = [1.0, 0.9, 0.8, 0.7]
            })
        });

        var json = JsonSerializer.Serialize(analysis, JsonWriterOptionsFactory.Create(pretty: true));

        json.Should().Contain("\"beat_provider\"");
        json.Should().Contain("\"shadow\"");
        json.Should().Contain("\"status\": \"not-configured\"");
    }

    private static TrackAnalysis CreateAnalysis(BeatProviderExportDiagnostics? beatProvider = null)
    {
        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = "hash",
                FilePath = "C:\\Music\\song.wav",
                DurationSeconds = 10.0,
                SampleRate = AnalysisOptions.DefaultTargetSampleRate,
                Tempo = 120.0,
                TimeSignature = AnalysisOptions.DefaultTimeSignature,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion,
                AnalyzedAt = DateTime.SpecifyKind(new DateTime(2026, 6, 16, 0, 0, 0), DateTimeKind.Utc)
            },
            Segments = [],
            Beats = [],
            Bars = [],
            Tatums = [],
            Sections = [],
            MicroFingerprints = [],
            Ai = null,
            BeatProvider = beatProvider ?? new BeatProviderExportDiagnostics
            {
                Name = "beat-this",
                Mode = "onnx-local",
                ModelName = "beat-this-large",
                ModelSha256 = "abc123",
                UsedAi = true,
                UsedBuiltIn = false,
                UsedFallback = false,
                DownbeatCount = 2,
                BeatNumberCount = 8,
                EstimatedMeter = 4,
                BeatGridMode = "beat-this-onnx-musical-v1",
                TatumMode = "fixed-two-per-beat",
                RequestedTatumMode = "Default",
                BarPhaseMode = "provider-downbeats"
            }
        };
    }
}
