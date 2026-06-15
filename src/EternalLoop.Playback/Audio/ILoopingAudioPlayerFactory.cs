using EternalLoop.Playback.Models;
using EternalLoop.Playback.Runtime;

namespace EternalLoop.Playback.Audio;

public interface ILoopingAudioPlayerFactory
{
    ILoopingAudioPlayer Create(
        LoadedAudio audio,
        RuntimeTrack track,
        BranchDecisionOptions? options = null);
}
