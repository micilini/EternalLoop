namespace EternalLoop.Core.Runtime;

public sealed record TrackRuntimeFileSet(
    string RunRoot,
    string AudioPath,
    string AnalysisJsonPath,
    string BranchesJsonPath);
