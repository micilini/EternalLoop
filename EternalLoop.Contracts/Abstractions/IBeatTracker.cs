using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;

namespace EternalLoop.Contracts.Abstractions;

public interface IBeatTracker
{
    BeatTrackingResult Track(LoadedAudio audio, FeatureMatrix features, BeatTrackingOptions options);
}
