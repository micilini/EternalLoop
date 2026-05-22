using EternalLoop.Contracts.Models;
using System.Threading;
using System.Threading.Tasks;

namespace EternalLoop.Contracts.Abstractions;

public interface ITrackAnalysisCache
{
    Task<TrackAnalysis?> TryGetAsync(string fileHash, CancellationToken cancellationToken);

    Task SaveAsync(TrackAnalysis analysis, CancellationToken cancellationToken);

    Task<TrackAnalysisCacheStats> GetStatsAsync(CancellationToken cancellationToken);

    Task ClearAsync();
}
