using EternalLoop.Core.Persistence;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Persistence;

public sealed class RecentFileListTests
{
    [Fact]
    public void Add_Should_PutNewFileAtTop()
    {
        var result = RecentFileList.Add("new.mp3", ["old.mp3"]);

        result.Should().Equal("new.mp3", "old.mp3");
    }

    [Fact]
    public void Add_Should_RemoveDuplicates_CaseInsensitive()
    {
        var result = RecentFileList.Add("Song.mp3", ["song.mp3", "other.mp3"]);

        result.Should().Equal("Song.mp3", "other.mp3");
    }

    [Fact]
    public void Add_Should_LimitToTenItems()
    {
        var result = RecentFileList.Add(
            "0.mp3",
            ["1.mp3", "2.mp3", "3.mp3", "4.mp3", "5.mp3", "6.mp3", "7.mp3", "8.mp3", "9.mp3", "10.mp3"]);

        result.Should().HaveCount(10);
        result.Should().Equal("0.mp3", "1.mp3", "2.mp3", "3.mp3", "4.mp3", "5.mp3", "6.mp3", "7.mp3", "8.mp3", "9.mp3");
    }

    [Fact]
    public void Normalize_Should_LimitToTenItems()
    {
        var result = RecentFileList.Normalize(["0.mp3", "1.mp3", "2.mp3", "3.mp3", "4.mp3", "5.mp3", "6.mp3", "7.mp3", "8.mp3", "9.mp3", "10.mp3"]);

        result.Should().HaveCount(10);
        result.Should().Equal("0.mp3", "1.mp3", "2.mp3", "3.mp3", "4.mp3", "5.mp3", "6.mp3", "7.mp3", "8.mp3", "9.mp3");
    }

    [Fact]
    public void Normalize_Should_RemoveEmptyValues()
    {
        var result = RecentFileList.Normalize(["a.mp3", "", " ", "a.mp3", "b.mp3"]);

        result.Should().Equal("a.mp3", "b.mp3");
    }
}
