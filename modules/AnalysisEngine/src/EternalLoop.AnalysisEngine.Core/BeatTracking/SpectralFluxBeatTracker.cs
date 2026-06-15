using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public sealed class SpectralFluxBeatTracker : IBeatTracker
{
    private const double FallbackBpm = 120.0;

    private const double FallbackConfidence = 0.35;

    private const float MinimumEnergy = 1e-9f;

    public BeatTrackingResult Track(
        LoadedAudio audio,
        FeatureMatrix features,
        BeatTrackingOptions options)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(options);

        if (audio.SampleRate <= 0)
        {
            throw new ArgumentException("Audio sample rate must be greater than zero.", nameof(audio));
        }

        if (features.HopLengthSamples <= 0)
        {
            throw new ArgumentException("Feature hop length must be greater than zero.", nameof(features));
        }

        var onsetSource = options.BeatMicroSnap && features.OnsetEnvelope.Length == features.SpectralFlux.Length
            ? features.OnsetEnvelope
            : features.SpectralFlux;

        if (features.SpectralFlux.Length == 0 || features.SpectralFlux.Max() <= MinimumEnergy)
        {
            var fallbackBpm = Math.Clamp(FallbackBpm, options.MinBpm, options.MaxBpm);
            return CreateFallbackResult(audio.DurationSeconds, fallbackBpm);
        }

        var tempoOdf = OnsetDetectionFunction.Build(features.SpectralFlux, options.OdfSmoothWindow);
        var odf = OnsetDetectionFunction.Build(onsetSource, options.OdfSmoothWindow);
        var tempoCandidates = TempoEstimator.EstimateCandidates(
            tempoOdf,
            features.HopLengthSamples,
            audio.SampleRate,
            options.MinBpm,
            options.MaxBpm,
            options.TempoCenterBpm,
            options.TempoPriorStdOctaves,
            options.HalfTimeCompetitivenessThreshold,
            30);
        var bpm = options.ForcedTempoBpm.HasValue
            ? Math.Clamp(options.ForcedTempoBpm.Value, options.MinBpm, options.MaxBpm)
            : options.UseGridTempoSelector
                ? TempoGridSelector.Select(
                    odf,
                    tempoCandidates.Candidates,
                    audio.SampleRate / (double)features.HopLengthSamples,
                    options.TightnessLambda).Bpm
                : tempoCandidates.SelectedCandidate?.Bpm ?? FallbackBpm;

        var framesPerSecond = audio.SampleRate / (double)features.HopLengthSamples;
        var targetPeriodFrames = framesPerSecond * 60.0 / bpm;
        var beatEvidence = options.EnableCompositeBeatTracking
            ? CompositeBeatEvidenceExtractor.Extract(features, options)
            : new CompositeBeatEvidence([], [], new Dictionary<string, double>());
        var beatFrames = options.BeatMicroSnap
            ? BeatAligner.AlignBeatsDynamicProgramming(odf, targetPeriodFrames, options.TightnessLambda)
            : BeatAligner.AlignBeats(odf, targetPeriodFrames, options.TightnessLambda);

        string beatGridMode;
        if (options.BeatMicroSnap && BeatGridRefiner.BeatGridQuality.Measure(beatFrames, targetPeriodFrames).IsUsable)
        {
            beatGridMode = "audio-refined";
        }
        else
        {
            beatFrames = BeatGridRefiner.EnsureUsableBeatGrid(
                odf,
                beatFrames,
                targetPeriodFrames,
                audio.DurationSeconds,
                framesPerSecond,
                options.BeatMicroSnap,
                options.BeatSnapWindowRatio,
                out beatGridMode);
        }

        CompositeDpBeatTrackingResult? compositeDp = null;
        if (options.EnableCompositeBeatTracking && beatEvidence.Composite.Length == odf.Length)
        {
            compositeDp = CompositeDpBeatTracker.Track(
                beatEvidence.Composite,
                beatFrames,
                targetPeriodFrames,
                options.TightnessLambda);
            if (compositeDp.Applied)
            {
                beatFrames = compositeDp.BeatFrames;
                beatGridMode = $"{beatGridMode}+composite-dp";
            }
        }

        ElasticBeatGridRefinementResult? elastic = null;
        if (options.EnableElasticBeatGrid)
        {
            elastic = ElasticBeatGridRefiner.Refine(
                odf,
                beatFrames,
                targetPeriodFrames,
                framesPerSecond,
                options.ElasticSearchWindowRatio,
                options.ElasticMaxShiftMs);
            if (elastic.Applied)
            {
                beatFrames = elastic.BeatFrames;
                beatGridMode = $"{beatGridMode}+elastic";
            }
        }

        PiecewiseBeatGridRefinementResult? piecewise = null;
        if (options.EnablePiecewiseBeatGrid)
        {
            piecewise = PiecewiseBeatGridRefiner.Refine(
                odf,
                beatFrames,
                targetPeriodFrames,
                framesPerSecond,
                options);
            if (piecewise.Applied)
            {
                beatFrames = piecewise.BeatFrames;
                beatGridMode = $"{beatGridMode}+piecewise";
            }
        }

        var pairs = beatFrames
            .Distinct()
            .Order()
            .Select(frame => new
            {
                Frame = frame,
                Time = frame * features.HopLengthSamples / (double)audio.SampleRate
            })
            .Where(item => item.Time >= 0 && item.Time <= audio.DurationSeconds)
            .ToArray();

        if (pairs.Length == 0)
        {
            return CreateFallbackResult(audio.DurationSeconds, bpm);
        }

        var maxOdf = Math.Max(MinimumEnergy, odf.Max());
        var evidenceValues = pairs
            .Select(item => beatEvidence.Composite.Length > 0 && item.Frame >= 0 && item.Frame < beatEvidence.Composite.Length
                ? (double)beatEvidence.Composite[item.Frame]
                : 0.0)
            .ToArray();

        return new BeatTrackingResult
        {
            EstimatedBpm = bpm,
            BeatTimes = pairs.Select(item => item.Time).ToArray(),
            Confidences = pairs
                .Select(item => item.Frame >= 0 && item.Frame < odf.Length
                    ? BuildConfidence(odf, item.Frame, maxOdf, options.EvidenceConfidences)
                    : FallbackConfidence)
                .ToArray(),
            BeatGridMode = beatGridMode,
            TempoCandidates = tempoCandidates.Candidates,
            ForcedTempoBpm = options.ForcedTempoBpm,
            ElasticRefinement = elastic,
            PiecewiseRefinement = piecewise,
            CompositeDpTracking = compositeDp,
            BeatEvidenceWeights = beatEvidence.Weights,
            BeatEvidenceMean = evidenceValues.Length > 0 ? evidenceValues.Average() : 0.0,
            BeatEvidenceVariance = CalculateVariance(evidenceValues),
            HpssRequested = options.UseHpss,
            HpssApplied = features.HpssApplied,
            HpssMode = features.HpssApplied ? options.HpssMode : "none",
            HpssAcceptedByGuardrails = options.UseHpss && features.HpssApplied && (compositeDp?.Applied ?? false),
            HpssRejectionReason = options.UseHpss && features.HpssApplied && !(compositeDp?.Applied ?? false)
                ? compositeDp?.Mode ?? "composite-dp-not-run"
                : null,
            BeatEvidenceSource = options.UseHpss && features.HpssApplied ? "full+percussive" : "full",
            TempoCandidateSources = options.UseHpss && features.HpssApplied ? ["full", "percussive"] : ["full"],
            SelectedTempoSource = "full"
        };
    }

    private static double BuildConfidence(float[] odf, int frame, float maxOdf, bool evidenceConfidences)
    {
        if (!evidenceConfidences)
        {
            return Math.Clamp(Math.Max(FallbackConfidence, (double)odf[frame]), 0.0, 1.0);
        }

        var start = Math.Max(0, frame - 2);
        var end = Math.Min(odf.Length - 1, frame + 2);
        var localMax = 0.0f;

        for (var index = start; index <= end; index++)
        {
            localMax = Math.Max(localMax, odf[index]);
        }

        return Math.Clamp(localMax / maxOdf, 0.0, 1.0);
    }

    private static BeatTrackingResult CreateFallbackResult(double durationSeconds, double bpm)
    {
        if (durationSeconds <= 0 || bpm <= 0 || !double.IsFinite(bpm))
        {
            return new BeatTrackingResult
            {
            EstimatedBpm = FallbackBpm,
            BeatTimes = [],
            Confidences = [],
            BeatGridMode = "regular",
            TempoCandidates = [],
            ForcedTempoBpm = null,
            HpssRequested = false,
            HpssApplied = false,
            HpssMode = "none"
            };
        }

        var beatDuration = 60.0 / bpm;
        var beatTimes = new List<double>();

        for (var time = 0.0; time <= durationSeconds; time += beatDuration)
        {
            beatTimes.Add(time);
        }

        return new BeatTrackingResult
        {
            EstimatedBpm = bpm,
            BeatTimes = beatTimes.ToArray(),
            Confidences = Enumerable.Repeat(FallbackConfidence, beatTimes.Count).ToArray(),
            BeatGridMode = "regular",
            TempoCandidates = [],
            ForcedTempoBpm = null,
            HpssRequested = false,
            HpssApplied = false,
            HpssMode = "none"
        };
    }

    private static double CalculateVariance(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0.0;
        }

        var mean = values.Average();
        return values.Sum(value => Math.Pow(value - mean, 2.0)) / values.Count;
    }
}
