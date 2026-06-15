using System.Diagnostics;
using EternalLoop.BranchAnalysis.Core.IO;

namespace EternalLoop.BranchAnalysis.Tests.Parity;

public sealed class NodeBranchAnalysisRunner
{
    public NodeBranchAnalysisRunner(string solutionRoot)
    {
        NodeRoot = Path.Combine(solutionRoot, "2. analysis-branchs");
        MainScript = Path.Combine(NodeRoot, "main.js");
    }

    public string NodeRoot { get; }
    public string MainScript { get; }
    public bool IsAvailable => File.Exists(MainScript);

    public string Run(string analysisRoot, string outputRoot, string trackName)
    {
        string arguments = string.Join(
            " ",
            [
                "main.js",
                "--analysis-root", Quote(analysisRoot),
                "--output-root", Quote(outputRoot),
                "--quantum-type", "beats",
                "--max-branches", "4",
                "--max-threshold", "80",
                "--force",
                "--pretty",
                "--quiet"
            ]);

        ProcessStartInfo startInfo = new()
        {
            FileName = "node",
            Arguments = arguments,
            WorkingDirectory = NodeRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start Node.js process.");

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            string command = $"node {arguments}";
            throw new InvalidOperationException(
                $"Node branch analysis failed.{Environment.NewLine}"
                + $"Command: {command}{Environment.NewLine}"
                + $"Exit code: {process.ExitCode}{Environment.NewLine}"
                + $"STDOUT:{Environment.NewLine}{stdout}{Environment.NewLine}"
                + $"STDERR:{Environment.NewLine}{stderr}");
        }

        return Path.Combine(outputRoot, trackName, BranchAnalysisWriter.BranchFileName);
    }

    private static string Quote(string value)
    {
        return $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }
}
