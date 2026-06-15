using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;

namespace EternalLoop.AnalysisEngine.Core.Features;

public sealed class NWavesAnalysisFeatureExtractor : IFeatureExtractor
{
    public FeatureMatrix Extract(LoadedAudio audio, FeatureExtractionOptions options)
    {
        FeatureExtractionValidation.Validate(audio, options);

        try
        {
            var rms = RmsExtractor.Compute(audio.Samples, options.FrameSize, options.HopLength);
            var samples = ApplyPreEmphasis(audio.Samples, options.PreEmphasis);
            var frames = StftAnalyzer.Analyze(samples, options.FrameSize, options.HopLength);

            var mfcc = ClassicMfccExtractor.Compute(
                frames,
                audio.SampleRate,
                options.FrameSize,
                options);

            var chroma = ChromaExtractor.Compute(
                frames,
                audio.SampleRate,
                options.FrameSize);

            var spectralFlux = SpectralFluxExtractor.Compute(frames);
            var onsetEnvelope = OnsetEnvelopeExtractor.Compute(frames, audio.SampleRate, options.FrameSize);
            var hpss = options.Hpss.UseHpss
                ? HpssSeparator.Separate(frames, options.Hpss)
                : null;
            var percussiveSpectralFlux = hpss is null ? [] : SpectralFluxExtractor.Compute(hpss.PercussiveFrames);
            var percussiveOnsetEnvelope = hpss is null ? [] : OnsetEnvelopeExtractor.Compute(hpss.PercussiveFrames, audio.SampleRate, options.FrameSize);
            var percussiveRms = hpss is null ? [] : SpectralRms(hpss.PercussiveFrames);
            var harmonicSpectralFlux = hpss is null ? [] : SpectralFluxExtractor.Compute(hpss.HarmonicFrames);
            var harmonicOnsetEnvelope = hpss is null ? [] : OnsetEnvelopeExtractor.Compute(hpss.HarmonicFrames, audio.SampleRate, options.FrameSize);
            var harmonicRms = hpss is null ? [] : SpectralRms(hpss.HarmonicFrames);
            var frameCount = new[] { mfcc.Length, chroma.Length, spectralFlux.Length, onsetEnvelope.Length, rms.Length }.Min();

            return new FeatureMatrix
            {
                Mfcc = Trim(mfcc, frameCount),
                Chroma = Trim(chroma, frameCount),
                SpectralFlux = spectralFlux.Take(frameCount).ToArray(),
                OnsetEnvelope = onsetEnvelope.Take(frameCount).ToArray(),
                PercussiveSpectralFlux = percussiveSpectralFlux.Take(frameCount).ToArray(),
                PercussiveOnsetEnvelope = percussiveOnsetEnvelope.Take(frameCount).ToArray(),
                PercussiveRms = percussiveRms.Take(frameCount).ToArray(),
                HarmonicSpectralFlux = harmonicSpectralFlux.Take(frameCount).ToArray(),
                HarmonicOnsetEnvelope = harmonicOnsetEnvelope.Take(frameCount).ToArray(),
                HarmonicRms = harmonicRms.Take(frameCount).ToArray(),
                HpssApplied = hpss is not null,
                HpssMode = hpss?.Mode ?? "none",
                HpssPercussiveEnergyRatio = hpss?.PercussiveEnergyRatio ?? 0.0,
                HpssHarmonicEnergyRatio = hpss?.HarmonicEnergyRatio ?? 0.0,
                Rms = rms.Take(frameCount).ToArray(),
                HopLengthSamples = options.HopLength,
                FrameSizeSamples = options.FrameSize,
                SampleRate = audio.SampleRate
            };
        }
        catch (FeatureExtractionException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not ArgumentException and not ArgumentOutOfRangeException)
        {
            throw new FeatureExtractionException("Feature extraction failed.", exception);
        }
    }

    private static float[] ApplyPreEmphasis(float[] samples, double coefficient)
    {
        if (samples.Length == 0)
        {
            return [];
        }

        if (coefficient <= 0.0)
        {
            return samples.ToArray();
        }

        var emphasized = new float[samples.Length];
        emphasized[0] = samples[0];

        for (var index = 1; index < samples.Length; index++)
        {
            emphasized[index] = (float)(samples[index] - (coefficient * samples[index - 1]));
        }

        return emphasized;
    }

    private static float[][] Trim(float[][] values, int count)
    {
        if (values.Length == count)
        {
            return values;
        }

        return values.Take(count).ToArray();
    }

    private static float[] SpectralRms(StftFrame[] frames)
    {
        var values = new float[frames.Length];
        for (var frame = 0; frame < frames.Length; frame++)
        {
            var power = frames[frame].PowerSpectrum.Sum(value => Math.Max(0.0f, value));
            values[frame] = (float)Math.Sqrt(power / Math.Max(1, frames[frame].PowerSpectrum.Length));
        }

        var max = values.Length == 0 ? 0.0f : values.Max();
        if (max <= 0.0f || !float.IsFinite(max))
        {
            return values;
        }

        for (var index = 0; index < values.Length; index++)
        {
            values[index] = Math.Clamp(values[index] / max, 0.0f, 1.0f);
        }

        return values;
    }
}
