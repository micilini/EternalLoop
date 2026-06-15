namespace EternalLoop.Core.Settings;

public interface IAnalysisCacheService
{
    Task<AnalysisCacheStats> GetStatsAsync(CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}

public sealed record AnalysisCacheStats(int FileCount, long TotalBytes);
