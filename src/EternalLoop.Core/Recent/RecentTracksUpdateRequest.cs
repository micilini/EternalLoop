using EternalLoop.Core.Cache;
using EternalLoop.Core.Runtime;

namespace EternalLoop.Core.Recent;

public sealed record RecentTracksUpdateRequest(
    TrackFileIdentity Identity,
    TrackRuntimePackage RuntimePackage,
    string RuntimeManifestPath,
    string RunRoot,
    DateTime UpdatedAtUtc);
