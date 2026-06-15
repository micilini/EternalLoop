using EternalLoop.Playback.Models;
using EternalLoop.Playback.Runtime;

namespace EternalLoop.Playback.Audio;

public sealed class LoopingAudioPlayerFactory : ILoopingAudioPlayerFactory
{
    private readonly IBranchRandomProvider _randomProvider;

    public LoopingAudioPlayerFactory(IBranchRandomProvider randomProvider)
    {
        _randomProvider = randomProvider;
    }

    public ILoopingAudioPlayer Create(
        LoadedAudio audio,
        RuntimeTrack track,
        BranchDecisionOptions? options = null)
    {
        BranchDecisionEngine engine = new(options ?? new BranchDecisionOptions(), _randomProvider);
        return new LoopingAudioPlayer(audio, track, engine, new BranchTransitionOptions());
    }
}
