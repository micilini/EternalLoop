using EternalLoop.Contracts.Models;

namespace EternalLoop.Core.AI;

public sealed class AiModelPathResolver
{
    private const string SolutionFileName = "EternalLoop.slnx";
    private const string AppProjectDirectoryName = "EternalLoop.App";
    private const string AssetsDirectoryName = "Assets";
    private const string ModelsDirectoryName = "Models";
    private const string DiscogsEffNetDirectoryName = "DiscogsEffNet";
    private const string ManifestFileName = "model-manifest.json";

    public string ResolveModelDirectory()
    {
        var baseDirectoryModelPath = Path.Combine(
            AppContext.BaseDirectory,
            AssetsDirectoryName,
            ModelsDirectoryName,
            DiscogsEffNetDirectoryName);

        if (Directory.Exists(baseDirectoryModelPath))
        {
            return baseDirectoryModelPath;
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                var repositoryModelPath = Path.Combine(
                    directory.FullName,
                    AppProjectDirectoryName,
                    AssetsDirectoryName,
                    ModelsDirectoryName,
                    DiscogsEffNetDirectoryName);

                if (Directory.Exists(repositoryModelPath))
                {
                    return repositoryModelPath;
                }
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"AI model directory '{DiscogsEffNetDirectoryName}' was not found from '{AppContext.BaseDirectory}'.");
    }

    public string ResolveManifestPath()
    {
        return RequireFile(Path.Combine(ResolveModelDirectory(), ManifestFileName), "AI model manifest");
    }

    public string ResolveOnnxPath(AiModelManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return RequireFile(Path.Combine(ResolveModelDirectory(), manifest.OnnxFile), "AI model ONNX file");
    }

    public string ResolveMetadataPath(AiModelManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return RequireFile(Path.Combine(ResolveModelDirectory(), manifest.MetadataFile), "AI model metadata file");
    }

    public string ResolveLicenseNoticePath(AiModelManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return RequireFile(Path.Combine(ResolveModelDirectory(), manifest.LicenseNoticeFile), "AI model license notice");
    }

    private static string RequireFile(string path, string description)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"{description} was not found at '{path}'.", path);
        }

        return path;
    }
}
