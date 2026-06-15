using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Tests.TestData;

internal static class TestSignalFactory
{
    public const int DefaultSampleRate = 22_050;

    public static LoadedAudio CreateSineLoadedAudio(
        double durationSeconds = 1.0,
        double frequency = 440.0,
        int sampleRate = DefaultSampleRate,
        float amplitude = 0.5f)
    {
        var sampleCount = Math.Max(1, (int)Math.Round(sampleRate * durationSeconds));
        var samples = new float[sampleCount];

        for (var index = 0; index < samples.Length; index++)
        {
            samples[index] = (float)(Math.Sin(2.0 * Math.PI * frequency * index / sampleRate) * amplitude);
        }

        return new LoadedAudio(
            samples,
            sampleRate,
            samples.Length / (double)sampleRate,
            "test-hash",
            "C:\\Tests\\sine.wav",
            "sine.wav");
    }

    public static LoadedAudio CreateSilentLoadedAudio(
        double durationSeconds = 1.0,
        int sampleRate = DefaultSampleRate)
    {
        var sampleCount = Math.Max(1, (int)Math.Round(sampleRate * durationSeconds));

        return new LoadedAudio(
            new float[sampleCount],
            sampleRate,
            sampleCount / (double)sampleRate,
            "test-hash",
            "C:\\Tests\\silence.wav",
            "silence.wav");
    }
}
