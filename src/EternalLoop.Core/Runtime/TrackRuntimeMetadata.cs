namespace EternalLoop.Core.Runtime;

public sealed record TrackRuntimeMetadata(
    string TrackId,
    string Title,
    string Artist,
    string FileHash,
    double DurationSeconds,
    double Tempo,
    int TimeSignature,
    string AnalysisSchemaVersion,
    int SettingsSchemaVersion,
    DateTime CreatedAtUtc);
