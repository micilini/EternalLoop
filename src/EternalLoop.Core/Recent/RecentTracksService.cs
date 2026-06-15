namespace EternalLoop.Core.Recent;

public sealed class RecentTracksService : IRecentTracksService
{
    public const int MaximumRecentTracks = 10;

    private readonly IRecentTracksRepository _repository;

    public RecentTracksService(IRecentTracksRepository repository)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IReadOnlyList<RecentTrackEntry>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        RecentTracksDocument document = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return document.Items
            .OrderByDescending(item => item.LastOpenedAtUtc)
            .Take(MaximumRecentTracks)
            .ToList();
    }

    public async Task<IReadOnlyList<RecentTrackEntry>> AddOrUpdateAsync(
        RecentTracksUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        RecentTracksDocument document = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        string normalizedPath = NormalizePath(request.Identity.FilePath);
        document.Items.RemoveAll(item => string.Equals(NormalizePath(item.FilePath), normalizedPath, StringComparison.OrdinalIgnoreCase));

        document.Items.Insert(0, new RecentTrackEntry
        {
            FilePath = request.Identity.FilePath,
            FileName = request.Identity.FileName,
            Folder = request.Identity.Folder,
            FileSizeBytes = request.Identity.FileSizeBytes,
            LastWriteTimeUtc = request.Identity.LastWriteTimeUtc,
            FileHash = request.Identity.Sha256,
            RuntimeManifestPath = request.RuntimeManifestPath,
            RunRoot = request.RunRoot,
            DurationSeconds = request.RuntimePackage.Metadata.DurationSeconds,
            Tempo = request.RuntimePackage.Metadata.Tempo,
            BeatCount = request.RuntimePackage.Summary.RuntimeBeatCount,
            BranchCount = request.RuntimePackage.Summary.RuntimeBranchCount,
            TuningPreset = request.RuntimePackage.Tuning.Preset,
            LastAnalyzedAtUtc = request.UpdatedAtUtc,
            LastOpenedAtUtc = request.UpdatedAtUtc
        });

        document.Items.RemoveRange(
            Math.Min(document.Items.Count, MaximumRecentTracks),
            Math.Max(0, document.Items.Count - MaximumRecentTracks));

        await _repository.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        return document.Items;
    }

    public async Task<IReadOnlyList<RecentTrackEntry>> RemoveMissingAsync(
        CancellationToken cancellationToken = default)
    {
        RecentTracksDocument document = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        document.Items.RemoveAll(item => !File.Exists(item.FilePath));
        await _repository.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        return document.Items;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        return _repository.SaveAsync(new RecentTracksDocument(), cancellationToken);
    }

    private static string NormalizePath(string filePath)
    {
        return string.IsNullOrWhiteSpace(filePath)
            ? string.Empty
            : Path.GetFullPath(filePath);
    }
}
