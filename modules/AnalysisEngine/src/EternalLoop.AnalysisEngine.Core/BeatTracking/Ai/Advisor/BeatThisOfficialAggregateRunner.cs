namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai.Advisor;

public sealed class BeatThisOfficialAggregateRunner
{
    private readonly BeatThisOfficialAggregateOptions _options;

    public BeatThisOfficialAggregateRunner(BeatThisOfficialAggregateOptions? options = null)
    {
        _options = options ?? new BeatThisOfficialAggregateOptions();
        _options.Validate();
    }

    public IReadOnlyList<int> BuildStarts(int frameCount)
    {
        if (frameCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameCount));
        }

        var starts = new List<int>();

        for (var start = _options.FirstStartFrame; start < frameCount; start += _options.HopFrames)
        {
            starts.Add(start);
        }

        var finalStart = frameCount - _options.ChunkFrames + _options.BorderFrames;

        if (finalStart > _options.FirstStartFrame && starts[^1] != finalStart)
        {
            starts[^1] = finalStart;
        }

        return starts;
    }

    public BeatThisAdvisorOutput Run(
        BeatThisSpectrogram spectrogram,
        IBeatModelRuntime runtime,
        BeatThisModelMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(spectrogram);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(metadata);

        if (spectrogram.MelBins != _options.MelBins)
        {
            throw new InvalidDataException(
                $"Beat This official aggregate expected {_options.MelBins} mel bins, got {spectrogram.MelBins}.");
        }

        var beat = new float[spectrogram.FrameCount];
        var downbeat = new float[spectrogram.FrameCount];
        var written = new bool[spectrogram.FrameCount];
        var starts = BuildStarts(spectrogram.FrameCount);
        var hopSize = Math.Max(1, (int)Math.Round(metadata.SampleRate / spectrogram.FrameRate));

        foreach (var start in starts)
        {
            var chunkData = new float[_options.ChunkFrames * _options.MelBins];

            for (var localFrame = 0; localFrame < _options.ChunkFrames; localFrame++)
            {
                var globalFrame = start + localFrame;

                if (globalFrame < 0 || globalFrame >= spectrogram.FrameCount)
                {
                    continue;
                }

                Array.Copy(
                    spectrogram.Data,
                    spectrogram.GetOffset(globalFrame, 0),
                    chunkData,
                    localFrame * _options.MelBins,
                    _options.MelBins);
            }

            var input = new BeatThisInputTensor(
                chunkData,
                [1, _options.ChunkFrames, _options.MelBins],
                _options.ChunkFrames,
                metadata.SampleRate,
                spectrogram.FrameRate,
                _options.ChunkFrames,
                _options.MelBins,
                metadata.FrameSize,
                hopSize,
                spectrogram.DurationSeconds);

            var output = runtime.Run(input, metadata);

            if (output.BeatActivations.Length < _options.ChunkFrames
                || output.DownbeatActivations.Length < _options.ChunkFrames)
            {
                throw new InvalidDataException("Beat This official aggregate runtime returned fewer frames than the requested chunk.");
            }

            for (var localFrame = _options.BorderFrames; localFrame < _options.ChunkFrames - _options.BorderFrames; localFrame++)
            {
                var globalFrame = start + localFrame;

                if (globalFrame < 0 || globalFrame >= spectrogram.FrameCount)
                {
                    continue;
                }

                if (written[globalFrame])
                {
                    continue;
                }

                beat[globalFrame] = output.BeatActivations[localFrame];
                downbeat[globalFrame] = output.DownbeatActivations[localFrame];
                written[globalFrame] = true;
            }
        }

        var gapCount = written.Count(value => !value);

        if (gapCount > 0)
        {
            throw new InvalidDataException($"Beat This official aggregate left {gapCount} frame(s) unwritten.");
        }

        return new BeatThisAdvisorOutput
        {
            BeatLogits = beat,
            DownbeatLogits = downbeat,
            FrameCount = spectrogram.FrameCount,
            FrameRate = spectrogram.FrameRate,
            DurationSeconds = spectrogram.DurationSeconds,
            ChunkCount = starts.Count,
            OutputMode = "official-aggregate-keep-first",
            AggregatePolicy = _options.AggregatePolicy
        };
    }
}
