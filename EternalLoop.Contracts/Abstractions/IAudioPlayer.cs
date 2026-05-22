using EternalLoop.Contracts.Events;
using EternalLoop.Contracts.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EternalLoop.Contracts.Abstractions;

public interface IAudioPlayer
{
    event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    event EventHandler<BeatChangedEventArgs>? BeatChanged;

    Task LoadAsync(LoadedAudio audio, IJukeboxEngine engine, CancellationToken cancellationToken);

    void Play();

    void Pause();

    void Stop();

    void Seek(TimeSpan position);

    TimeSpan Position { get; }

    float Volume { get; set; }
}
