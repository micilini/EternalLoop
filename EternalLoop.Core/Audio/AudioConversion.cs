using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace EternalLoop.Core.Audio;

public static class AudioConversion
{
    public static ISampleProvider ToMono(ISampleProvider source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.WaveFormat.Channels switch
        {
            1 => source,
            2 => new StereoToMonoSampleProvider(source)
            {
                LeftVolume = 0.5f,
                RightVolume = 0.5f
            },
            _ => new MultiChannelToMonoSampleProvider(source)
        };
    }

    public static float[] ReadAllSamples(
        ISampleProvider source,
        CancellationToken cancellationToken,
        int readBufferSize = 16_384,
        TimeSpan? maxDuration = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (readBufferSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(readBufferSize), "Read buffer size must be greater than zero.");
        }

        var maxSamples = maxDuration.HasValue
            ? (long)Math.Ceiling(maxDuration.Value.TotalSeconds * source.WaveFormat.SampleRate * source.WaveFormat.Channels)
            : long.MaxValue;

        var samples = new List<float>();
        var buffer = new float[readBufferSize];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var read = source.Read(buffer, 0, buffer.Length);
            if (read <= 0)
            {
                break;
            }

            if (samples.Count + read > maxSamples)
            {
                throw new AudioLoadException($"Audio exceeds the maximum allowed duration of {maxDuration!.Value.TotalMinutes:0.#} minutes.");
            }

            for (var i = 0; i < read; i++)
            {
                samples.Add(buffer[i]);
            }
        }

        return samples.ToArray();
    }
}
