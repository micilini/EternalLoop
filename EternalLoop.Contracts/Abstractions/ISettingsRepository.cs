using EternalLoop.Contracts.Models;
using System.Threading;
using System.Threading.Tasks;

namespace EternalLoop.Contracts.Abstractions;

public interface ISettingsRepository
{
    Task<UserSettings> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(UserSettings settings, CancellationToken cancellationToken);
}
