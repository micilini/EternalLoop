using EternalLoop.Contracts.Models;

namespace EternalLoop.Core.AI;

public sealed class AiAudioPreprocessor
{
    public float[] ResampleToModelRate(LoadedAudio audio, int targetSampleRate)
    {
        ArgumentNullException.ThrowIfNull(audio);

        if (targetSampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetSampleRate));
        }

        if (audio.SampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(audio));
        }

        if (audio.Samples is null)
        {
            throw new ArgumentException("Audio samples are required.", nameof(audio));
        }

        if (audio.Samples.Length == 0)
        {
            return [];
        }

        if (audio.SampleRate == targetSampleRate)
        {
            return audio.Samples.Select(Sanitize).ToArray();
        }

        var targetLength = Math.Max(1, (int)Math.Round(audio.Samples.Length * targetSampleRate / (double)audio.SampleRate));
        var sourceLength = audio.Samples.Length;
        var output = new float[targetLength];

        if (sourceLength == 1 || targetLength == 1)
        {
            Array.Fill(output, Sanitize(audio.Samples[0]));
            return output;
        }

        var sourceSpan = sourceLength - 1;
        var targetSpan = targetLength - 1;

        for (var targetIndex = 0; targetIndex < targetLength; targetIndex++)
        {
            var sourcePosition = targetIndex * sourceSpan / (double)targetSpan;
            var left = (int)Math.Floor(sourcePosition);
            var right = Math.Min(left + 1, sourceLength - 1);
            var fraction = sourcePosition - left;
            var leftSample = Sanitize(audio.Samples[left]);
            var rightSample = Sanitize(audio.Samples[right]);
            output[targetIndex] = (float)(leftSample + (rightSample - leftSample) * fraction);
        }

        return output;
    }

    private static float Sanitize(float value)
    {
        return float.IsFinite(value) ? value : 0.0f;
    }
}
