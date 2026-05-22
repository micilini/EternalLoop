using EternalLoop.Contracts.Options;
using EternalLoop.Core.Analysis;
using EternalLoop.Core.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Analysis;

public sealed class ChromaExtractorTests
{
    [Fact]
    public void Extract_Should_ConcentrateChromaAroundA_For440HzTone()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(
            durationSeconds: 1.0,
            frequency: 440.0);

        var extractor = new NWavesFeatureExtractor();

        var features = extractor.Extract(audio, new FeatureExtractionOptions
        {
            ComputeDeltas = false
        });

        var averageChroma = Average(features.Chroma);
        var strongestPitchClass = Array.IndexOf(averageChroma, averageChroma.Max());

        strongestPitchClass.Should().Be(9);
    }

    [Fact]
    public void Compute_Should_SuppressSingleFramePitchClassSpike_WithMedianFilter()
    {
        const int sampleRate = 22_050;
        const int frameSize = 2048;
        var frames = new[]
        {
            CreateFrameWithPitchClass(9, sampleRate, frameSize),
            CreateFrameWithPitchClass(9, sampleRate, frameSize),
            CreateFrameWithPitchClass(0, sampleRate, frameSize),
            CreateFrameWithPitchClass(9, sampleRate, frameSize),
            CreateFrameWithPitchClass(9, sampleRate, frameSize)
        };

        var chroma = ChromaExtractor.Compute(frames, sampleRate, frameSize);

        var centerFrame = chroma[2];
        var strongestPitchClass = Array.IndexOf(centerFrame, centerFrame.Max());

        strongestPitchClass.Should().Be(9);
        centerFrame[0].Should().BeApproximately(0.0f, 0.0001f);
    }

    [Fact]
    public void Compute_Should_PreserveConsistentPitchClassChange()
    {
        const int sampleRate = 22_050;
        const int frameSize = 2048;
        var frames = new[]
        {
            CreateFrameWithPitchClass(9, sampleRate, frameSize),
            CreateFrameWithPitchClass(9, sampleRate, frameSize),
            CreateFrameWithPitchClass(0, sampleRate, frameSize),
            CreateFrameWithPitchClass(0, sampleRate, frameSize),
            CreateFrameWithPitchClass(0, sampleRate, frameSize),
            CreateFrameWithPitchClass(0, sampleRate, frameSize),
            CreateFrameWithPitchClass(0, sampleRate, frameSize)
        };

        var chroma = ChromaExtractor.Compute(frames, sampleRate, frameSize);

        var laterFrame = chroma[5];
        var strongestPitchClass = Array.IndexOf(laterFrame, laterFrame.Max());

        strongestPitchClass.Should().Be(0);
    }

    [Fact]
    public void Compute_Should_NormalizeFramesAfterMedianFiltering()
    {
        const int sampleRate = 22_050;
        const int frameSize = 2048;
        var frames = new[]
        {
            CreateFrameWithPitchClass(9, sampleRate, frameSize),
            CreateFrameWithPitchClass(9, sampleRate, frameSize),
            CreateFrameWithPitchClass(0, sampleRate, frameSize),
            CreateFrameWithPitchClass(9, sampleRate, frameSize),
            CreateFrameWithPitchClass(9, sampleRate, frameSize)
        };

        var chroma = ChromaExtractor.Compute(frames, sampleRate, frameSize);

        chroma.Should().AllSatisfy(frame =>
        {
            frame.Should().HaveCount(ChromaExtractor.PitchClassCount);
            frame.Max().Should().BeApproximately(1.0f, 0.0001f);
        });
    }

    [Fact]
    public void Compute_Should_ReturnEmpty_WhenNoFramesAreProvided()
    {
        var chroma = ChromaExtractor.Compute([], sampleRate: 22_050, frameSize: 2048);

        chroma.Should().BeEmpty();
    }

    private static float[] Average(float[][] vectors)
    {
        vectors.Should().NotBeEmpty();

        var result = new float[vectors[0].Length];

        foreach (var vector in vectors)
        {
            for (var i = 0; i < vector.Length; i++)
            {
                result[i] += vector[i];
            }
        }

        for (var i = 0; i < result.Length; i++)
        {
            result[i] /= vectors.Length;
        }

        return result;
    }

    private static int FindBinForPitchClass(int pitchClass, int sampleRate, int frameSize)
    {
        var binCount = frameSize / 2 + 1;

        for (var bin = 1; bin < binCount; bin++)
        {
            var frequency = bin * sampleRate / (double)frameSize;

            if (frequency < 50.0 || frequency > 5000.0)
            {
                continue;
            }

            var currentPitchClass =
                (int)Math.Round(12.0 * Math.Log2(frequency / 440.0) + 9.0) % ChromaExtractor.PitchClassCount;

            if (currentPitchClass < 0)
            {
                currentPitchClass += ChromaExtractor.PitchClassCount;
            }

            if (currentPitchClass == pitchClass)
            {
                return bin;
            }
        }

        throw new InvalidOperationException($"Could not find FFT bin for pitch class {pitchClass}.");
    }

    private static StftFrame CreateFrameWithPitchClass(int pitchClass, int sampleRate = 22_050, int frameSize = 2048)
    {
        var magnitudes = new float[frameSize / 2 + 1];
        var bin = FindBinForPitchClass(pitchClass, sampleRate, frameSize);
        magnitudes[bin] = 1.0f;

        return new StftFrame
        {
            Magnitudes = magnitudes,
            PowerSpectrum = magnitudes.Select(value => value * value).ToArray()
        };
    }
}
