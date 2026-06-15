using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.Options;

public sealed class AnalysisOptions
{
    public const int DefaultTargetSampleRate = 22050;

    public const int DefaultTimeSignature = 4;

    public const string DefaultArtist = "Local";

    public int TargetSampleRate { get; init; } = DefaultTargetSampleRate;

    public int TimeSignature { get; init; } = DefaultTimeSignature;

    public string SchemaVersion { get; init; } = TrackAnalysis.CurrentSchemaVersion;

    public string Artist { get; init; } = DefaultArtist;

    public MusicalQualityOptions MusicalQuality { get; init; } = new();

    public AnalysisTuningOptions Tuning { get; init; } = new();
}

public sealed class MusicalQualityOptions
{
    public bool AcousticSegmentation { get; init; } = false;

    public bool BeatMicroSnap { get; init; } = false;

    public bool AdaptiveTatums { get; init; } = false;

    public bool StructuralSections { get; init; } = false;

    public bool EvidenceConfidences { get; init; } = false;

    public static MusicalQualityOptions AllEnabled { get; } = new()
    {
        AcousticSegmentation = true,
        BeatMicroSnap = true,
        AdaptiveTatums = true,
        StructuralSections = true,
        EvidenceConfidences = true
    };
}

public sealed class AnalysisTuningOptions
{
    public double? StartBpmPriorCenter { get; init; }

    public double? BpmPriorStdOctaves { get; init; }

    public double? MinTempo { get; init; }

    public double? MaxTempo { get; init; }

    public double? HalfTimeCompetitivenessThreshold { get; init; }

    public double? BeatDpTightness { get; init; }

    public double? BeatSnapMaxMilliseconds { get; init; }

    public double? ForcedTempoBpm { get; init; }

    public double? BeatEvidenceLogMelOnsetWeight { get; init; }

    public double? BeatEvidenceLowBandOnsetWeight { get; init; }

    public double? BeatEvidenceMidBandOnsetWeight { get; init; }

    public double? BeatEvidenceHighBandOnsetWeight { get; init; }

    public double? BeatEvidenceRmsDeltaWeight { get; init; }

    public double? BeatEvidenceMfccDeltaWeight { get; init; }

    public double? BeatEvidenceChromaDeltaWeight { get; init; }

    public double? BeatEvidenceNoveltyWeight { get; init; }

    public bool? UseHpss { get; init; }

    public string? HpssMode { get; init; }

    public int? HpssTimeMedianKernelFrames { get; init; }

    public int? HpssFrequencyMedianKernelBins { get; init; }

    public double? HpssMaskPower { get; init; }

    public double? HpssPercussiveMargin { get; init; }

    public double? HpssHarmonicMargin { get; init; }

    public double? FullMixOnsetWeight { get; init; }

    public double? PercussiveOnsetWeight { get; init; }

    public double? HarmonicOnsetWeight { get; init; }
}
