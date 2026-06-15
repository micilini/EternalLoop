using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.Audio;

public interface IAudioLoader
{
    Task<LoadedAudio> LoadAsync(
        string filePath,
        int targetSampleRate,
        CancellationToken cancellationToken);
}
