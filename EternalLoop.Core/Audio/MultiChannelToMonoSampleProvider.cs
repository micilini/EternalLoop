using NAudio.Wave;

namespace EternalLoop.Core.Audio;

internal sealed class MultiChannelToMonoSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly int _channels;
    private float[] _sourceBuffer;

    public MultiChannelToMonoSampleProvider(ISampleProvider source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.WaveFormat.Channels <= 1)
        {
            throw new ArgumentException("Source must have more than one channel.", nameof(source));
        }

        _source = source;
        _channels = source.WaveFormat.Channels;
        _sourceBuffer = Array.Empty<float>();
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 1);
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (count <= 0)
        {
            return 0;
        }

        var requestedSourceSamples = count * _channels;
        EnsureBufferSize(requestedSourceSamples);

        var sourceSamplesRead = _source.Read(_sourceBuffer, 0, requestedSourceSamples);
        var framesRead = sourceSamplesRead / _channels;

        for (var frame = 0; frame < framesRead; frame++)
        {
            float sum = 0;

            var sourceOffset = frame * _channels;
            for (var channel = 0; channel < _channels; channel++)
            {
                sum += _sourceBuffer[sourceOffset + channel];
            }

            buffer[offset + frame] = sum / _channels;
        }

        return framesRead;
    }

    private void EnsureBufferSize(int minimumLength)
    {
        if (_sourceBuffer.Length >= minimumLength)
        {
            return;
        }

        _sourceBuffer = new float[minimumLength];
    }
}
