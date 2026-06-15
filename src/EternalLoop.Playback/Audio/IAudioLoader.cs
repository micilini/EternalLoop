namespace EternalLoop.Playback.Audio;

public interface IAudioLoader
{
    Task<LoadedAudio> LoadAsync(string audioPath, CancellationToken cancellationToken = default);
}
