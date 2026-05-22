using EternalLoop.Contracts.Models;

namespace EternalLoop.Core.Tests.TestData;

internal static class TestSignalFactory
{
    public static LoadedAudio CreateSineLoadedAudio(
        int sampleRate = 22_050,
        double durationSeconds = 1.0,
        double frequency = 440.0)
    {
        var samples = CreateSineSamples(sampleRate, durationSeconds, frequency);

        return new LoadedAudio(
            samples,
            sampleRate,
            durationSeconds,
            "test-hash");
    }

    public static LoadedAudio CreateSilenceThenToneLoadedAudio(
        int sampleRate = 22_050,
        double silenceSeconds = 0.5,
        double toneSeconds = 0.5,
        double frequency = 440.0)
    {
        var silenceLength = (int)(sampleRate * silenceSeconds);
        var tone = CreateSineSamples(sampleRate, toneSeconds, frequency);
        var samples = new float[silenceLength + tone.Length];

        Array.Copy(tone, 0, samples, silenceLength, tone.Length);

        return new LoadedAudio(
            samples,
            sampleRate,
            silenceSeconds + toneSeconds,
            "test-onset-hash");
    }

    public static LoadedAudio CreateImpulseLoadedAudio(
        int sampleRate = 22_050,
        double durationSeconds = 1.0,
        double impulseAtSeconds = 0.5)
    {
        var length = (int)(sampleRate * durationSeconds);
        var samples = new float[length];
        var impulseIndex = Math.Clamp((int)(sampleRate * impulseAtSeconds), 0, length - 1);

        samples[impulseIndex] = 1.0f;

        return new LoadedAudio(
            samples,
            sampleRate,
            durationSeconds,
            "test-impulse-hash");
    }

    public static LoadedAudio CreateClickTrackLoadedAudio(
        int sampleRate = 22_050,
        double durationSeconds = 8.0,
        double bpm = 120.0,
        double clickDurationSeconds = 0.01)
    {
        var length = (int)(sampleRate * durationSeconds);
        var samples = new float[length];

        var intervalSeconds = 60.0 / bpm;
        var clickSamples = Math.Max(1, (int)(sampleRate * clickDurationSeconds));

        for (var beatTime = 0.0; beatTime < durationSeconds; beatTime += intervalSeconds)
        {
            var start = (int)(beatTime * sampleRate);

            for (var i = 0; i < clickSamples && start + i < samples.Length; i++)
            {
                var t = i / (double)Math.Max(1, clickSamples - 1);
                var envelope = 1.0 - t;
                samples[start + i] += (float)(0.9 * envelope);
            }
        }

        return new LoadedAudio(
            samples,
            sampleRate,
            durationSeconds,
            $"click-track-{bpm:0}");
    }

    private static float[] CreateSineSamples(int sampleRate, double durationSeconds, double frequency)
    {
        var length = (int)(sampleRate * durationSeconds);
        var samples = new float[length];

        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = (float)(Math.Sin(2.0 * Math.PI * frequency * i / sampleRate) * 0.5);
        }

        return samples;
    }
}
