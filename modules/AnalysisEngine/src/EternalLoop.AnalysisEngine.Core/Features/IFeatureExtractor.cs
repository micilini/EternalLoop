using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;

namespace EternalLoop.AnalysisEngine.Core.Features;

public interface IFeatureExtractor
{
    FeatureMatrix Extract(LoadedAudio audio, FeatureExtractionOptions options);
}
