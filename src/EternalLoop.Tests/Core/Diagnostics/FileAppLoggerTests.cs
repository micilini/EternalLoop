using EternalLoop.Core.Diagnostics;
using EternalLoop.Core.Settings;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Diagnostics;

public sealed class FileAppLoggerTests
{
    [Fact]
    public void LogShouldWriteLineToAppDataLogs()
    {
        string root = Directory.CreateTempSubdirectory("eternalloop-logs-").FullName;

        try
        {
            var provider = new AppPathProvider(root);
            var logger = new FileAppLogger(provider);

            logger.Log(AppLogLevel.Error, "Test failure.", new InvalidOperationException("boom"));

            string[] files = Directory.GetFiles(provider.LogsDirectory, "eternalloop-*.log");
            files.Should().ContainSingle();
            File.ReadAllText(files[0]).Should().Contain("[Error]").And.Contain("Test failure.").And.Contain("InvalidOperationException");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
