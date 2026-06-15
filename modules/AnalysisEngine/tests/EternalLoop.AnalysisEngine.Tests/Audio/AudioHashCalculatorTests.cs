using EternalLoop.AnalysisEngine.Core.Audio;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Audio;

public sealed class AudioHashCalculatorTests
{
    [Fact]
    public async Task Sha256Async_returns_same_hash_for_same_file()
    {
        var directory = CreateTempDirectory();
        var path = Path.Combine(directory, "sample.txt");
        await File.WriteAllTextAsync(path, "eternalloop-analysis-exporter");

        var first = await AudioHashCalculator.Sha256Async(path, CancellationToken.None);
        var second = await AudioHashCalculator.Sha256Async(path, CancellationToken.None);

        first.Should().Be(second);
        first.Should().HaveLength(64);
        first.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public async Task Sha256Async_returns_different_hash_for_different_files()
    {
        var directory = CreateTempDirectory();
        var firstPath = Path.Combine(directory, "first.txt");
        var secondPath = Path.Combine(directory, "second.txt");

        await File.WriteAllTextAsync(firstPath, "first");
        await File.WriteAllTextAsync(secondPath, "second");

        var first = await AudioHashCalculator.Sha256Async(firstPath, CancellationToken.None);
        var second = await AudioHashCalculator.Sha256Async(secondPath, CancellationToken.None);

        first.Should().NotBe(second);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "EternalLoopAnalysisEngineTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(path);
        return path;
    }
}
