namespace EternalLoop.BranchAnalysis.Core.IO;

public static class AnalysisDiscovery
{
    public const string AnalysisFileName = "eternalloop-analysis.json";

    public static IReadOnlyList<AnalysisDiscoveryResult> Discover(string analysisRoot)
    {
        string resolvedRoot = Path.GetFullPath(analysisRoot);

        if (!Directory.Exists(resolvedRoot))
        {
            throw new AnalysisRootNotFoundException(resolvedRoot);
        }

        return Directory
            .EnumerateDirectories(resolvedRoot)
            .Select(directoryPath => new
            {
                Name = Path.GetFileName(directoryPath),
                DirectoryPath = Path.GetFullPath(directoryPath),
                AnalysisPath = Path.GetFullPath(Path.Combine(directoryPath, AnalysisFileName))
            })
            .Where(analysis => File.Exists(analysis.AnalysisPath))
            .OrderBy(analysis => analysis.Name, StringComparer.Ordinal)
            .Select(analysis => new AnalysisDiscoveryResult
            {
                Name = analysis.Name,
                DirectoryPath = analysis.DirectoryPath,
                AnalysisPath = analysis.AnalysisPath
            })
            .ToArray();
    }
}
