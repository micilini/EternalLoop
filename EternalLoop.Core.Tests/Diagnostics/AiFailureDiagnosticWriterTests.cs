using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;

namespace EternalLoop.Core.Tests.Diagnostics;

public sealed class AiFailureDiagnosticWriterTests : IDisposable
{
    private const string SourceFilePath = "C:\\Music\\track.mp3";
    private const string FileHash = "abcdef1234567890";
    private const double DurationSeconds = 12.5;
    private const int SampleRate = 22050;
    private const int SampleCount = 256;
    private readonly string _root = Path.Combine(Path.GetTempPath(), "EternalLoopAiFailureDiagnostics", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Write_creates_ai_failure_log_file()
    {
        var writer = CreateWriter();

        var path = writer.Write(SourceFilePath, CreateAudio(), CreateBeats(), new BranchFindingOptions(), CreateExceptionWithStackTrace());

        File.Exists(path).Should().BeTrue();
        Path.GetExtension(path).Should().Be(".log");
    }

    [Fact]
    public void Write_includes_exception_to_string()
    {
        var writer = CreateWriter();

        var path = writer.Write(SourceFilePath, CreateAudio(), CreateBeats(), new BranchFindingOptions(), CreateExceptionWithStackTrace());
        var text = File.ReadAllText(path);

        text.Should().Contain(nameof(IndexOutOfRangeException));
        text.Should().Contain("Synthetic AI index failure.");
        text.Should().Contain(nameof(ThrowNestedException));
        text.Should().Contain("Exception.ToString()");
    }

    [Fact]
    public void Write_includes_audio_and_beat_context()
    {
        var writer = CreateWriter();

        var path = writer.Write(SourceFilePath, CreateAudio(), CreateBeats(), new BranchFindingOptions(), CreateExceptionWithStackTrace());
        var text = File.ReadAllText(path);

        text.Should().Contain("SourceFilePath: " + SourceFilePath);
        text.Should().Contain("FileHash: " + FileHash);
        text.Should().Contain("DurationSeconds: " + DurationSeconds.ToString(CultureInfo.InvariantCulture));
        text.Should().Contain("SampleRate: " + SampleRate);
        text.Should().Contain("SampleCount: " + SampleCount);
        text.Should().Contain("BeatCount: " + CreateBeats().Length);
        text.Should().Contain("UseAiSimilarity: True");
    }

    [Fact]
    public void Write_creates_unique_files_for_multiple_failures()
    {
        var writer = CreateWriter();

        var first = writer.Write(SourceFilePath, CreateAudio(), CreateBeats(), new BranchFindingOptions(), CreateExceptionWithStackTrace());
        var second = writer.Write(SourceFilePath, CreateAudio(), CreateBeats(), new BranchFindingOptions(), CreateExceptionWithStackTrace());

        second.Should().NotBe(first);
        File.Exists(first).Should().BeTrue();
        File.Exists(second).Should().BeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private AiFailureDiagnosticWriter CreateWriter()
    {
        return new AiFailureDiagnosticWriter(
            new FakeAppPathProvider(_root),
            NullLogger<AiFailureDiagnosticWriter>.Instance);
    }

    private static LoadedAudio CreateAudio()
    {
        return new LoadedAudio(
            Enumerable.Repeat(0.1f, SampleCount).ToArray(),
            SampleRate,
            DurationSeconds,
            FileHash);
    }

    private static Beat[] CreateBeats()
    {
        return
        [
            new Beat
            {
                Index = 0,
                Start = 0.0,
                Duration = 0.5,
                Confidence = 1.0,
                Timbre = [1.0f],
                Pitches = [1.0f],
                Loudness = [0.0f],
                BarPosition = [0.0f]
            }
        ];
    }

    private static Exception CreateExceptionWithStackTrace()
    {
        try
        {
            ThrowNestedException();
            throw new InvalidOperationException("Unreachable");
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static void ThrowNestedException()
    {
        throw new IndexOutOfRangeException("Synthetic AI index failure.");
    }

    private sealed class FakeAppPathProvider : IAppPathProvider
    {
        public FakeAppPathProvider(string root)
        {
            AppDataDirectory = root;
            CacheDirectory = Path.Combine(root, "Cache");
            LogsDirectory = Path.Combine(root, "Logs");
            SettingsFilePath = Path.Combine(root, "settings.json");
        }

        public string AppDataDirectory { get; }

        public string CacheDirectory { get; }

        public string LogsDirectory { get; }

        public string SettingsFilePath { get; }

        public void EnsureDirectories()
        {
            Directory.CreateDirectory(AppDataDirectory);
            Directory.CreateDirectory(CacheDirectory);
            Directory.CreateDirectory(LogsDirectory);
        }
    }
}
