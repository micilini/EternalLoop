namespace EternalLoop.Core.Settings;

public interface IUserSettingsRepository
{
    Task<EternalLoopUserSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(EternalLoopUserSettings settings, CancellationToken cancellationToken = default);
}
