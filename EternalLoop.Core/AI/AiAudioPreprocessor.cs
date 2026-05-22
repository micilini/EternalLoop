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

        var targetLength = CalculateTargetLength(audio.Samples.LongLength, audio.SampleRate, targetSampleRate);
        var sourceLength = audio.Samples.Length;
        var output = new float[targetLength];

        if (sourceLength == 1 || targetLength == 1)
        {
            Array.Fill(output, Sanitize(audio.Samples[0]));
            return output;
        }

        var sourceSpan = sourceLength - 1;
        var targetSpan = targetLength - 1;
        var sourceScale = sourceSpan / (double)targetSpan;

        for (var targetIndex = 0; targetIndex < targetLength; targetIndex++)
        {
            var sourcePosition = targetIndex * sourceScale;
            var left = Math.Clamp((int)Math.Floor(sourcePosition), 0, sourceLength - 1);
            var right = Math.Min(left + 1, sourceLength - 1);
            var fraction = sourcePosition - left;
            var leftSample = Sanitize(audio.Samples[left]);
            var rightSample = Sanitize(audio.Samples[right]);
            output[targetIndex] = (float)(leftSample + (rightSample - leftSample) * fraction);
        }

        return output;
    }

    private static int CalculateTargetLength(long sourceLength, int sourceSampleRate, int targetSampleRate)
    {
        var targetLength = Math.Round(sourceLength * (double)targetSampleRate / sourceSampleRate);

        if (targetLength > Array.MaxLength)
        {
            throw new InvalidOperationException("AI resampling output is too large for a single array.");
        }

        return Math.Max(1, (int)targetLength);
    }

    private static float Sanitize(float value)
    {
        return float.IsFinite(value) ? value : 0.0f;
    }
}
