using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;

namespace EternalLoop.Contracts.Abstractions;

public interface IFeatureExtractor
{
    FeatureMatrix Extract(LoadedAudio audio, FeatureExtractionOptions options);
}
