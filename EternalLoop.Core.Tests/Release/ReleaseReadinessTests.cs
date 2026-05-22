using System.Text.Json;
using System.Xml.Linq;
using EternalLoop.Contracts;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Release;

public sealed class ReleaseReadinessTests
{
    private const string ExpectedVersion = "1.1.0";
    private const string ExpectedDisplayVersion = "v1.1.0";
    private const string ExpectedInputName = "melspectrogram";
    private const string ExpectedOutputName = "embeddings";
    private const int ExpectedEmbeddingDimensions = 512;
    private const string RuntimePackageName = "Microsoft.ML.OnnxRuntime";
    private const string DirectMlPackageName = "Microsoft.ML.OnnxRuntime.DirectML";
    private const string ModelDirectory = "Assets\\Models\\DiscogsEffNet";
    private const string OnnxFileName = "discogs_track_embeddings-effnet-bs64-1.onnx";
    private const string MetadataFileName = "discogs_track_embeddings-effnet-bs64-1.json";
    private const string ManifestFileName = "model-manifest.json";
    private const string NoticeFileName = "MODEL-LICENSE-NOTICE.txt";

    [Fact]
    public void ProductInfo_reports_v1_1_0()
    {
        ProductInfo.Version.Should().Be(ExpectedVersion);
        ProductInfo.DisplayVersion.Should().Be(ExpectedDisplayVersion);
    }

    [Fact]
    public void App_project_version_is_1_1_0()
    {
        var document = XDocument.Load(GetPath("EternalLoop.App", "EternalLoop.App.csproj"));

        document.Descendants("Version").Single().Value.Should().Be(ExpectedVersion);
        document.Descendants("AssemblyVersion").Single().Value.Should().Be("1.1.0.0");
        document.Descendants("FileVersion").Single().Value.Should().Be("1.1.0.0");
        document.Descendants("InformationalVersion").Single().Value.Should().Be(ExpectedVersion);
    }

    [Fact]
    public void Readme_mentions_v1_1_0()
    {
        var readme = ReadText("README.md");

        readme.Should().Contain(ExpectedVersion);
        readme.Should().Contain("Local AI");
        readme.Should().Contain("512");
        readme.Should().Contain(NoticeFileName);
        readme.Should().NotContain("EternalLoop For Windows (1.0.0)");
    }

    [Fact]
    public void Readme_does_not_claim_model_is_mit()
    {
        var readme = ReadText("README.md");

        readme.Should().NotContain("model files are MIT licensed");
        readme.Should().NotContain("model is MIT licensed");
        readme.Should().Contain("model files are third-party assets");
    }

    [Fact]
    public void Third_party_notices_exists()
    {
        File.Exists(GetPath("THIRD-PARTY-NOTICES.md")).Should().BeTrue();
    }

    [Fact]
    public void Model_manifest_uses_runtime_onnx_contract()
    {
        using var document = JsonDocument.Parse(ReadText("EternalLoop.App", "Assets", "Models", "DiscogsEffNet", ManifestFileName));
        var root = document.RootElement;

        root.GetProperty("inputName").GetString().Should().Be(ExpectedInputName);
        root.GetProperty("embeddingOutputName").GetString().Should().Be(ExpectedOutputName);
        root.GetProperty("embeddingDimensions").GetInt32().Should().Be(ExpectedEmbeddingDimensions);
    }

    [Fact]
    public void Model_license_notice_exists()
    {
        var notice = ReadText("EternalLoop.App", "Assets", "Models", "DiscogsEffNet", NoticeFileName);

        notice.Should().Contain("Discogs-EffNet");
        notice.Should().Contain("MTG / Essentia");
        notice.Should().Contain("CC BY-NC-SA 4.0");
        notice.Should().Contain("third-party assets");
        notice.Should().Contain("MIT");
    }

    [Fact]
    public void App_project_copies_model_assets_to_publish()
    {
        var project = XDocument.Load(GetPath("EternalLoop.App", "EternalLoop.App.csproj"));

        var modelContent = project.Descendants("Content")
            .Single(element => element.Attribute("Include")?.Value == $"{ModelDirectory}\\**\\*.*");

        modelContent.Element("CopyToPublishDirectory")?.Value.Should().Be("Always");
    }

    [Fact]
    public void App_project_validates_model_files_before_publish()
    {
        var project = ReadText("EternalLoop.App", "EternalLoop.App.csproj");

        project.Should().Contain("ValidateAiModelFiles");
        project.Should().Contain(OnnxFileName);
        project.Should().Contain(MetadataFileName);
        project.Should().Contain(ManifestFileName);
        project.Should().Contain(NoticeFileName);
    }

    [Fact]
    public void Core_project_uses_cpu_onnx_runtime_package()
    {
        var project = XDocument.Load(GetPath("EternalLoop.Core", "EternalLoop.Core.csproj"));

        var packageReferences = project.Descendants("PackageReference")
            .Where(element => string.Equals(element.Attribute("Include")?.Value, RuntimePackageName, StringComparison.Ordinal))
            .ToList();

        packageReferences.Should().ContainSingle();
    }

    [Fact]
    public void Core_project_does_not_reference_directml()
    {
        var project = ReadText("EternalLoop.Core", "EternalLoop.Core.csproj");

        project.Should().NotContain(DirectMlPackageName);
        project.Should().NotContain("OnnxRuntime.Gpu");
    }

    [Fact]
    public void Gitignore_excludes_downloaded_model_files()
    {
        var gitignore = ReadText(".gitignore");

        gitignore.Should().Contain($"EternalLoop.App/Assets/Models/DiscogsEffNet/{OnnxFileName}");
        gitignore.Should().Contain($"EternalLoop.App/Assets/Models/DiscogsEffNet/{MetadataFileName}");
    }

    [Fact]
    public void Gitignore_does_not_exclude_manifest_or_notice()
    {
        var gitignore = ReadText(".gitignore");

        gitignore.Should().NotContain(ManifestFileName);
        gitignore.Should().NotContain(NoticeFileName);
    }

    private static string ReadText(params string[] parts)
    {
        return File.ReadAllText(GetPath(parts));
    }

    private static string GetPath(params string[] parts)
    {
        return Path.Combine([FindRepositoryRoot(), .. parts]);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "EternalLoop.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find EternalLoop.slnx.");
    }
}
