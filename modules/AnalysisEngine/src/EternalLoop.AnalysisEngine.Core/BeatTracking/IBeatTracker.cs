using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public interface IBeatTracker
{
    BeatTrackingResult Track(
        LoadedAudio audio,
        FeatureMatrix features,
        BeatTrackingOptions options);
}
