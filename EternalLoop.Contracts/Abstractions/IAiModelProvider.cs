using EternalLoop.Contracts.Models;
using System.Threading;
using System.Threading.Tasks;

namespace EternalLoop.Contracts.Abstractions;

public interface IAiModelProvider
{
    Task<AiModelManifest> GetManifestAsync(CancellationToken cancellationToken);
}
