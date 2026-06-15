namespace EternalLoop.Core.Cache;

public sealed record TrackFileIdentity(
    string FilePath,
    string FileName,
    string Folder,
    long FileSizeBytes,
    DateTime LastWriteTimeUtc,
    string Sha256);
