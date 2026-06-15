namespace EternalLoop.Core.Recent;

public interface IRecentTracksRepository
{
    Task<RecentTracksDocument> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(RecentTracksDocument document, CancellationToken cancellationToken = default);
}
