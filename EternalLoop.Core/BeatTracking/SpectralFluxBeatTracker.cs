using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;

namespace EternalLoop.Core.BeatTracking;

public sealed class SpectralFluxBeatTracker : IBeatTracker
{
    private const double FallbackBpm = 120.0;

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

        ArgumentNullException.ThrowIfNull(features.SpectralFlux);

        if (features.SpectralFlux.Length == 0)
        {
            return new BeatTrackingResult
            {
                EstimatedBpm = Math.Clamp(FallbackBpm, options.MinBpm, options.MaxBpm),
                BeatTimes = [],
                Confidences = []
            };
        }

        var odf = OnsetDetectionFunction.Build(features.SpectralFlux, options.OdfSmoothWindow);
        var bpm = TempoEstimator.EstimateBpm(
            odf,
            features.HopLengthSamples,
            audio.SampleRate,
            options.MinBpm,
            options.MaxBpm);

        var framesPerSecond = audio.SampleRate / (double)features.HopLengthSamples;
        var targetPeriodFrames = framesPerSecond * 60.0 / bpm;
        var beatFrames = BeatAligner.AlignBeats(
            odf,
            targetPeriodFrames,
            options.TightnessLambda);
        beatFrames = BeatGridRefiner.EnsureUsableBeatGrid(
            odf,
            beatFrames,
            targetPeriodFrames,
            audio.DurationSeconds,
            framesPerSecond);

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

        return new BeatTrackingResult
        {
            EstimatedBpm = bpm,
            BeatTimes = pairs.Select(item => item.Time).ToArray(),
            Confidences = pairs
                .Select(item => item.Frame >= 0 && item.Frame < odf.Length
                    ? Math.Clamp(Math.Max(0.35, (double)odf[item.Frame]), 0.0, 1.0)
                    : 0.35)
                .ToArray()
        };
    }
}
