using EternalLoop.App.Tests.TestDoubles;
using EternalLoop.App.ViewModels;
using EternalLoop.Core.Recent;
using FluentAssertions;
using System.IO;

namespace EternalLoop.App.Tests.ViewModels;

public sealed class RecentTracksViewModelTests
{
    [Fact]
    public async Task LoadAsyncShouldPopulateRecentTracks()
    {
        using var temp = new TempAudioFile();
        var service = new FakeRecentTracksService([CreateEntry(temp.Path)]);
        var viewModel = new RecentTracksViewModel(service, _ => { }, () => { });

        await AsyncTest.EventuallyAsync(() =>
        {
            viewModel.RecentTracks.Should().ContainSingle();
            viewModel.HasRecentTracks.Should().BeTrue();
            viewModel.StatusMessage.Should().Be("1 recent track(s).");
        });
    }

    [Fact]
    public async Task LoadAsyncFailureShouldShowFriendlyStatus()
    {
        var service = new FakeRecentTracksService(loadException: new IOException("load failed"));
        var viewModel = new RecentTracksViewModel(service, _ => { }, () => { });

        await AsyncTest.EventuallyAsync(() =>
        {
            viewModel.RecentTracks.Should().BeEmpty();
            viewModel.StatusMessage.Should().Be("Recent tracks could not be loaded.");
            viewModel.HasRecentTracks.Should().BeFalse();
        });
    }

    [Fact]
    public async Task OpenTrackCommandShouldOpenExistingTrack()
    {
        using var temp = new TempAudioFile();
        string? openedPath = null;
        var viewModel = new RecentTracksViewModel(
            new FakeRecentTracksService([CreateEntry(temp.Path)]),
            path => openedPath = path,
            () => { });
        await AsyncTest.EventuallyAsync(() => viewModel.RecentTracks.Should().ContainSingle());

        viewModel.OpenTrackCommand.Execute(viewModel.RecentTracks[0]);

        openedPath.Should().Be(temp.Path);
    }

    [Fact]
    public async Task OpenTrackCommandShouldRefuseMissingTrackWithFriendlyStatus()
    {
        string? openedPath = null;
        string missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.mp3");
        var viewModel = new RecentTracksViewModel(
            new FakeRecentTracksService([CreateEntry(missingPath)]),
            path => openedPath = path,
            () => { });
        await AsyncTest.EventuallyAsync(() => viewModel.RecentTracks.Should().ContainSingle());

        viewModel.OpenTrackCommand.Execute(viewModel.RecentTracks[0]);

        openedPath.Should().BeNull();
        viewModel.StatusMessage.Should().Be("Original file was not found.");
    }

    [Fact]
    public async Task RemoveMissingCommandShouldRefreshCollection()
    {
        using var temp = new TempAudioFile();
        var service = new FakeRecentTracksService([CreateEntry(temp.Path)])
        {
            RemoveMissingResult = []
        };
        var viewModel = new RecentTracksViewModel(service, _ => { }, () => { });
        await AsyncTest.EventuallyAsync(() => viewModel.RecentTracks.Should().ContainSingle());

        viewModel.RemoveMissingCommand.Execute(null);

        await AsyncTest.EventuallyAsync(() =>
        {
            viewModel.RecentTracks.Should().BeEmpty();
            viewModel.StatusMessage.Should().Be("Missing tracks removed.");
            viewModel.HasRecentTracks.Should().BeFalse();
        });
    }

    [Fact]
    public async Task RemoveMissingCommandFailureShouldShowFriendlyStatus()
    {
        using var temp = new TempAudioFile();
        var service = new FakeRecentTracksService([CreateEntry(temp.Path)])
        {
            RemoveMissingException = new IOException("remove failed")
        };
        var viewModel = new RecentTracksViewModel(service, _ => { }, () => { });
        await AsyncTest.EventuallyAsync(() => viewModel.RecentTracks.Should().ContainSingle());

        viewModel.RemoveMissingCommand.Execute(null);

        await AsyncTest.EventuallyAsync(() =>
            viewModel.StatusMessage.Should().Be("Recent tracks could not be updated."));
    }

    [Fact]
    public void BackCommandShouldInvokeBackCallback()
    {
        bool backInvoked = false;
        var viewModel = new RecentTracksViewModel(new FakeRecentTracksService([]), _ => { }, () => backInvoked = true);

        viewModel.BackCommand.Execute(null);

        backInvoked.Should().BeTrue();
    }

    private static RecentTrackEntry CreateEntry(string path)
    {
        return new RecentTrackEntry
        {
            FilePath = path,
            FileName = Path.GetFileName(path),
            Folder = Path.GetDirectoryName(path) ?? string.Empty,
            LastOpenedAtUtc = DateTime.UtcNow,
            LastAnalyzedAtUtc = DateTime.UtcNow
        };
    }

    private sealed class TempAudioFile : IDisposable
    {
        public TempAudioFile()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.mp3");
            File.WriteAllBytes(Path, [0]);
        }

        public string Path { get; }

        public void Dispose()
        {
            File.Delete(Path);
        }
    }

    private sealed class FakeRecentTracksService(
        IReadOnlyList<RecentTrackEntry>? loadEntries = null,
        Exception? loadException = null)
        : IRecentTracksService
    {
        private readonly IReadOnlyList<RecentTrackEntry> _loadEntries = loadEntries ?? [];

        public IReadOnlyList<RecentTrackEntry> RemoveMissingResult { get; init; } = loadEntries ?? [];

        public Exception? RemoveMissingException { get; init; }

        public Task<IReadOnlyList<RecentTrackEntry>> LoadAsync(CancellationToken cancellationToken = default)
        {
            if (loadException is not null)
            {
                throw loadException;
            }

            return Task.FromResult(_loadEntries);
        }

        public Task<IReadOnlyList<RecentTrackEntry>> AddOrUpdateAsync(
            RecentTracksUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_loadEntries);
        }

        public Task<IReadOnlyList<RecentTrackEntry>> RemoveMissingAsync(CancellationToken cancellationToken = default)
        {
            if (RemoveMissingException is not null)
            {
                throw RemoveMissingException;
            }

            return Task.FromResult(RemoveMissingResult);
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
