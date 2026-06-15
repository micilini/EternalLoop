using EternalLoop.AnalysisEngine.Core.Options;
using EternalLoop.BranchAnalysis.Core.Runner;

namespace EternalLoop.Core.Settings;

public static class LoopTuningOptionsMapper
{
    public static AnalysisOptions ToAnalysisOptions(LoopTuningSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new AnalysisOptions
        {
            Artist = AnalysisOptions.DefaultArtist,
            MusicalQuality = settings.AnalysisMusicalQuality
                ? MusicalQualityOptions.AllEnabled
                : new MusicalQualityOptions()
        };
    }

    public static BranchAnalysisOptions ToBranchAnalysisOptions(
        LoopTuningSettings settings,
        bool force,
        bool pretty,
        bool quiet)
    {
        ArgumentNullException.ThrowIfNull(settings);

        BranchAnalysisOptions options = BranchAnalysisOptions.CreateDefault();

        options.QuantumType = string.IsNullOrWhiteSpace(settings.BranchQuantumType)
            ? "beats"
            : settings.BranchQuantumType;
        options.SimilarityThreshold = Clamp(settings.SimilarityThreshold, 0.65, 0.95);
        options.LookaheadDepth = Clamp(settings.LookaheadDepth, 1, 5);
        options.MinJumpDistance = Clamp(settings.MinJumpDistance, 4, 64);
        options.MaxBranches = Clamp(settings.MaxBranchesPerBeat, 1, 12);
        options.MaxThreshold = BranchAnalysisTuningMapper.MapSimilarityToMaxThreshold(
            options.SimilarityThreshold);
        options.Force = force;
        options.Pretty = pretty;
        options.Quiet = quiet;

        return options;
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (!double.IsFinite(value))
        {
            return minimum;
        }

        return Math.Min(Math.Max(value, minimum), maximum);
    }
}
