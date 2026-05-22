using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;

namespace EternalLoop.Core.Tests.AI;

public sealed class AiModelAssetTests
{
    private const string SolutionFileName = "EternalLoop.slnx";
    private const string AppProjectRelativePath = "EternalLoop.App/EternalLoop.App.csproj";
    private const string ModelDirectoryRelativePath = "EternalLoop.App/Assets/Models/DiscogsEffNet";
    private const string ManifestFileName = "model-manifest.json";
    private const string LicenseNoticeFileName = "MODEL-LICENSE-NOTICE.txt";
    private const string OnnxFileName = "discogs_track_embeddings-effnet-bs64-1.onnx";
    private const string MetadataFileName = "discogs_track_embeddings-effnet-bs64-1.json";
    private const string ExpectedModelId = "discogs-track-effnet-bs64-v1";
    private const string ExpectedInputName = "serving_default_melspectrogram";
    private const string ExpectedEmbeddingOutputName = "PartitionedCall:1";
    private const string ExpectedLicense = "CC BY-NC-SA 4.0";
    private const int ExpectedBatchSize = 64;
    private const int ExpectedMelBands = 128;
    private const int ExpectedPatchFrames = 96;
    private const int ExpectedEmbeddingDimensions = 1280;
    private const int ExpectedSampleRate = 16000;
    private const long MinimumOnnxBytes = 10000000;
    private const long MinimumMetadataBytes = 1000;
    private const int HtmlProbeByteCount = 512;

    private static readonly string[] RequiredManifestFields =
    [
        "id",
        "displayName",
        "provider",
        "version",
        "onnxFile",
        "metadataFile",
        "licenseNoticeFile",
        "inputName",
        "embeddingOutputName",
        "batchSize",
        "melBands",
        "patchFrames",
        "embeddingDimensions",
        "sampleRate",
        "license",
        "source",
        "onnxUrl",
        "metadataUrl"
    ];

    [Fact]
    public void Model_manifest_has_required_fields()
    {
        using var manifest = LoadManifest();

        foreach (var field in RequiredManifestFields)
        {
            manifest.RootElement.TryGetProperty(field, out _).Should().BeTrue($"manifest must include {field}");
        }
    }

    [Fact]
    public void Model_manifest_has_expected_discogs_effnet_values()
    {
        using var manifest = LoadManifest();
        var root = manifest.RootElement;

        root.GetProperty("id").GetString().Should().Be(ExpectedModelId);
        root.GetProperty("onnxFile").GetString().Should().Be(OnnxFileName);
        root.GetProperty("metadataFile").GetString().Should().Be(MetadataFileName);
        root.GetProperty("licenseNoticeFile").GetString().Should().Be(LicenseNoticeFileName);
        root.GetProperty("inputName").GetString().Should().Be(ExpectedInputName);
        root.GetProperty("embeddingOutputName").GetString().Should().Be(ExpectedEmbeddingOutputName);
        root.GetProperty("batchSize").GetInt32().Should().Be(ExpectedBatchSize);
        root.GetProperty("melBands").GetInt32().Should().Be(ExpectedMelBands);
        root.GetProperty("patchFrames").GetInt32().Should().Be(ExpectedPatchFrames);
        root.GetProperty("embeddingDimensions").GetInt32().Should().Be(ExpectedEmbeddingDimensions);
        root.GetProperty("sampleRate").GetInt32().Should().Be(ExpectedSampleRate);
        root.GetProperty("license").GetString().Should().Be(ExpectedLicense);
    }

    [Fact]
    public void Model_license_notice_exists_and_mentions_third_party_license()
    {
        var noticePath = Path.Combine(ModelDirectoryPath, LicenseNoticeFileName);

        File.Exists(noticePath).Should().BeTrue();

        var notice = File.ReadAllText(noticePath);
        notice.Should().Contain(ExpectedLicense);
        notice.Should().Contain("MIT");
        notice.Should().Contain("third-party assets");
        notice.Should().Contain("must not download this model at runtime");
    }

    [Fact]
    public void App_project_includes_model_assets_as_content()
    {
        var document = XDocument.Load(AppProjectPath);
        var contentIncludes = document.Descendants("Content")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => value is not null)
            .Cast<string>()
            .ToArray();

        contentIncludes.Should().Contain(@"Assets\Models\DiscogsEffNet\**\*.*");
    }

    [Fact]
    public void App_project_validates_ai_model_files_before_publish()
    {
        var document = XDocument.Load(AppProjectPath);
        var target = document.Descendants("Target")
            .SingleOrDefault(element => element.Attribute("Name")?.Value == "ValidateAiModelFiles");

        target.Should().NotBeNull();

        var projectXml = File.ReadAllText(AppProjectPath);
        projectXml.Should().Contain("DiscogsEffNetOnnxPath");
        projectXml.Should().Contain("DiscogsEffNetMetadataPath");
        projectXml.Should().Contain("DiscogsEffNetManifestPath");
        projectXml.Should().Contain("DiscogsEffNetLicenseNoticePath");
        projectXml.Should().Contain("tools/download-ai-models.ps1");
    }

    [Fact]
    public void Downloaded_model_files_are_valid_when_present()
    {
        var onnxPath = Path.Combine(ModelDirectoryPath, OnnxFileName);
        var metadataPath = Path.Combine(ModelDirectoryPath, MetadataFileName);

        if (!File.Exists(onnxPath) && !File.Exists(metadataPath))
        {
            return;
        }

        File.Exists(onnxPath).Should().BeTrue();
        File.Exists(metadataPath).Should().BeTrue();

        new FileInfo(onnxPath).Length.Should().BeGreaterThan(MinimumOnnxBytes);
        new FileInfo(metadataPath).Length.Should().BeGreaterThan(MinimumMetadataBytes);

        File.ReadAllText(metadataPath).TrimStart().Should().StartWith("{");
        ReadFilePrefix(onnxPath).Should().NotContain("<html");
    }

    private static string RepositoryRootPath => GetRepositoryRoot().FullName;

    private static string AppProjectPath => Path.Combine(RepositoryRootPath, AppProjectRelativePath);

    private static string ModelDirectoryPath => Path.Combine(RepositoryRootPath, ModelDirectoryRelativePath);

    private static JsonDocument LoadManifest()
    {
        var manifestPath = Path.Combine(ModelDirectoryPath, ManifestFileName);
        File.Exists(manifestPath).Should().BeTrue();
        return JsonDocument.Parse(File.ReadAllText(manifestPath));
    }

    private static string ReadFilePrefix(string path)
    {
        using var stream = File.OpenRead(path);
        var bytesToRead = Math.Min(HtmlProbeByteCount, (int)stream.Length);
        var buffer = new byte[bytesToRead];
        var bytesRead = stream.Read(buffer, 0, bytesToRead);
        return Encoding.UTF8.GetString(buffer, 0, bytesRead).ToLowerInvariant();
    }

    private static DirectoryInfo GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new DirectoryNotFoundException($"Could not locate {SolutionFileName} from {AppContext.BaseDirectory}");
        }

        return directory;
    }
}
