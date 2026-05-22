using EternalLoop.Core.Hashing;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Audio;

public sealed class FileHasherTests
{
    [Fact]
    public async Task Sha256Async_Should_ReturnSameHash_ForSameFile()
    {
        var directory = CreateTempDirectory();
        var path = Path.Combine(directory, "sample.txt");
        await File.WriteAllTextAsync(path, "eternalloop");

        var first = await FileHasher.Sha256Async(path, CancellationToken.None);
        var second = await FileHasher.Sha256Async(path, CancellationToken.None);

        first.Should().Be(second);
        first.Should().HaveLength(64);
        first.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "EternalLoopTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
