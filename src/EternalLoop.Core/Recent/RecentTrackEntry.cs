namespace EternalLoop.Core.Recent;

public sealed class RecentTrackEntry
{
    public string FilePath { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string Folder { get; init; } = string.Empty;

    public long FileSizeBytes { get; init; }

    public DateTime LastWriteTimeUtc { get; init; }

    public string FileHash { get; init; } = string.Empty;

    public string RuntimeManifestPath { get; init; } = string.Empty;

    public string RunRoot { get; init; } = string.Empty;

    public double DurationSeconds { get; init; }

    public double Tempo { get; init; }

    public int BeatCount { get; init; }

    public int BranchCount { get; init; }

    public string TuningPreset { get; init; } = string.Empty;

    public DateTime LastAnalyzedAtUtc { get; init; }

    public DateTime LastOpenedAtUtc { get; init; }
}
