using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace EternalLoop.AnalysisEngine.Core.Audio;

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
            _ => new AverageChannelsToMonoSampleProvider(source)
        };
    }

    public static float[] ReadAllSamples(
        ISampleProvider source,
        CancellationToken cancellationToken,
        int readBufferSize,
        TimeSpan? maxDuration)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (readBufferSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(readBufferSize),
                "Read buffer size must be greater than zero.");
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
                throw new AudioLoadingException(
                    $"Audio exceeds the maximum allowed duration of {maxDuration!.Value.TotalMinutes:0.#} minutes.");
            }

            for (var index = 0; index < read; index++)
            {
                samples.Add(buffer[index]);
            }
        }

        return samples.ToArray();
    }

    private sealed class AverageChannelsToMonoSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly int _channels;
        private readonly WaveFormat _waveFormat;

        public AverageChannelsToMonoSampleProvider(ISampleProvider source)
        {
            _source = source;
            _channels = source.WaveFormat.Channels;
            _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 1);
        }

        public WaveFormat WaveFormat => _waveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            var sourceFramesRequested = count;
            var sourceSamplesRequested = sourceFramesRequested * _channels;
            var sourceBuffer = new float[sourceSamplesRequested];
            var sourceSamplesRead = _source.Read(sourceBuffer, 0, sourceBuffer.Length);
            var framesRead = sourceSamplesRead / _channels;

            for (var frame = 0; frame < framesRead; frame++)
            {
                var sum = 0f;

                for (var channel = 0; channel < _channels; channel++)
                {
                    sum += sourceBuffer[(frame * _channels) + channel];
                }

                buffer[offset + frame] = sum / _channels;
            }

            return framesRead;
        }
    }
}
