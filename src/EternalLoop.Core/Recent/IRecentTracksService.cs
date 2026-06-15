namespace EternalLoop.Core.Recent;

public interface IRecentTracksService
{
    Task<IReadOnlyList<RecentTrackEntry>> LoadAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecentTrackEntry>> AddOrUpdateAsync(
        RecentTracksUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecentTrackEntry>> RemoveMissingAsync(CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
