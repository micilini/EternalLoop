namespace EternalLoop.AnalysisEngine.Tests.TestData;

internal static class TestWaveFileFactory
{
    public static string CreateSineWaveFile(
        string directory,
        string fileName,
        int sampleRate = 44_100,
        int channels = 2,
        double durationSeconds = 1.0,
        double frequency = 440.0)
    {
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, fileName);
        var totalFrames = (int)(sampleRate * durationSeconds);
        var totalSamples = totalFrames * channels;

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        const short bitsPerSample = 16;
        const short audioFormatPcm = 1;

        var byteRate = sampleRate * channels * bitsPerSample / 8;
        short blockAlign = (short)(channels * bitsPerSample / 8);
        var dataSize = totalSamples * bitsPerSample / 8;
        var riffChunkSize = 36 + dataSize;

        writer.Write("RIFF"u8.ToArray());
        writer.Write(riffChunkSize);
        writer.Write("WAVE"u8.ToArray());

        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write(audioFormatPcm);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);

        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        for (var frame = 0; frame < totalFrames; frame++)
        {
            var sample = Math.Sin(2.0 * Math.PI * frequency * frame / sampleRate);
            var pcm = (short)Math.Clamp(
                sample * short.MaxValue * 0.25,
                short.MinValue,
                short.MaxValue);

            for (var channel = 0; channel < channels; channel++)
            {
                writer.Write(pcm);
            }
        }

        return path;
    }

    public static string CreateTextFile(string directory, string fileName)
    {
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, "This is not an audio file.");

        return path;
    }
}
