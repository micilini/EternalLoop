namespace EternalLoop.Playback.Audio;

public interface ILoopingAudioPlayer : IDisposable
{
    event EventHandler<BeatChangedEventArgs>? BeatChanged;

    event EventHandler<BranchJumpEventArgs>? BranchJumped;

    event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    event EventHandler? PlaybackCompleted;

    PlaybackState State { get; }

    int CurrentBeatIndex { get; }

    double PositionSeconds { get; }

    double DurationSeconds { get; }

    void Play();

    void Pause();

    void Stop();

    void Seek(double seconds);

    void SetBringItHome(bool enabled);
}
