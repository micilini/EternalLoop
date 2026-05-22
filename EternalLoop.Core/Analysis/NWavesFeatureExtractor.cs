using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;

namespace EternalLoop.Core.Analysis;

public sealed class NWavesFeatureExtractor : IFeatureExtractor
{
    public FeatureMatrix Extract(LoadedAudio audio, FeatureExtractionOptions options)
    {
        FeatureExtractionValidation.Validate(audio, options);

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

        var frameCount = new[] { mfcc.Length, chroma.Length, spectralFlux.Length, rms.Length }.Min();

        return new FeatureMatrix
        {
            Mfcc = Trim(mfcc, frameCount),
            Chroma = Trim(chroma, frameCount),
            SpectralFlux = spectralFlux.Take(frameCount).ToArray(),
            Rms = rms.Take(frameCount).ToArray(),
            HopLengthSamples = options.HopLength,
            FrameSizeSamples = options.FrameSize
        };
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

        for (var i = 1; i < samples.Length; i++)
        {
            emphasized[i] = (float)(samples[i] - coefficient * samples[i - 1]);
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
}
