using EternalLoop.Contracts.Models;
using System.Threading;
using System.Threading.Tasks;

namespace EternalLoop.Contracts.Abstractions;

public interface IAiEmbeddingExtractor
{
    Task<AiEmbeddingExtractionResult> ExtractAsync(
        LoadedAudio audio,
        IAnalysisProgressReporter progressReporter,
        CancellationToken cancellationToken);
}
