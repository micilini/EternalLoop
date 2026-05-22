namespace EternalLoop.App.Services;

public interface ITuningService
{
    Task<TuningApplyResult> ApplyAsync(CancellationToken cancellationToken);
}
