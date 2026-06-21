namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;

public static class BeatThisFullTrackInferenceMerger
{
    public static BeatThisInferenceResult Merge(IReadOnlyList<BeatThisInferenceResult> chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        if (chunks.Count == 0)
        {
            throw new InvalidDataException("Cannot merge empty Beat This inference chunk list.");
        }

        var frameRate = chunks[0].FrameRate;

        if (frameRate <= 0.0)
        {
            throw new InvalidDataException("Beat This chunk frame rate must be positive.");
        }

        foreach (var chunk in chunks)
        {
            if (Math.Abs(chunk.FrameRate - frameRate) > 1e-9)
            {
                throw new InvalidDataException("Cannot merge Beat This chunks with different frame rates.");
            }

            if (chunk.ValidFrameCount < 0)
            {
                throw new InvalidDataException("Beat This chunk valid frame count cannot be negative.");
            }

            if (chunk.BeatActivations.Length < chunk.ValidFrameCount
                || chunk.DownbeatActivations.Length < chunk.ValidFrameCount)
            {
                throw new InvalidDataException("Beat This chunk activation length is smaller than valid frame count.");
            }
        }

        var totalFrameCount = chunks.Max(chunk => chunk.StartFrameIndex + chunk.ValidFrameCount);

        if (totalFrameCount <= 0)
        {
            throw new InvalidDataException("Beat This full-track merge did not produce valid frames.");
        }

        var beatActivations = new float[totalFrameCount];
        var downbeatActivations = new float[totalFrameCount];
        var writeCounts = new int[totalFrameCount];

        foreach (var chunk in chunks)
        {
            for (var index = 0; index < chunk.ValidFrameCount; index++)
            {
                var targetIndex = chunk.StartFrameIndex + index;

                beatActivations[targetIndex] += chunk.BeatActivations[index];
                downbeatActivations[targetIndex] += chunk.DownbeatActivations[index];
                writeCounts[targetIndex]++;
            }
        }

        for (var index = 0; index < totalFrameCount; index++)
        {
            if (writeCounts[index] <= 1)
            {
                continue;
            }

            beatActivations[index] /= writeCounts[index];
            downbeatActivations[index] /= writeCounts[index];
        }

        return new BeatThisInferenceResult
        {
            BeatActivations = beatActivations,
            DownbeatActivations = downbeatActivations,
            FrameRate = frameRate,
            ValidFrameCount = totalFrameCount,
            StartFrameIndex = 0,
            StartTimeSeconds = 0.0,
            AudioDurationSeconds = chunks.Max(chunk => chunk.AudioDurationSeconds),
            ChunkCount = chunks.Count,
            OutputNames = chunks
                .SelectMany(chunk => chunk.OutputNames)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            OutputMode = $"full-track-chunked:{string.Join("+", chunks.Select(chunk => chunk.OutputMode).Distinct())}"
        };
    }
}
