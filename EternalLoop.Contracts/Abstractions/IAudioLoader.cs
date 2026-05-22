using EternalLoop.Contracts.Models;
using System.Threading;
using System.Threading.Tasks;

namespace EternalLoop.Contracts.Abstractions;

public interface IAudioLoader
{
    Task<LoadedAudio> LoadAsync(string filePath, int targetSampleRate, CancellationToken cancellationToken);
}
