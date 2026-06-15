using EternalLoop.BranchAnalysis.Core.IO;
using EternalLoop.BranchAnalysis.Core.Runner;

namespace EternalLoop.BranchAnalysis.Tests.Parity;

public static class CSharpBranchAnalysisRunner
{
    public static string Run(string analysisRoot, string outputRoot, string trackName)
    {
        BranchAnalysisOptions options = BranchAnalysisOptions.CreateDefault();
        options.AnalysisRoot = analysisRoot;
        options.OutputRoot = outputRoot;
        options.QuantumType = "beats";
        options.MaxBranches = 4;
        options.MaxThreshold = 80;
        options.Force = true;
        options.Pretty = true;
        options.Quiet = true;

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = BranchAnalysisRunner.Run(options, output, error);

        if (exitCode != BranchAnalysisRunnerExitCodes.Success)
        {
            throw new InvalidOperationException($"C# branch analysis failed with exit code {exitCode}.{Environment.NewLine}{error}");
        }

        return Path.Combine(outputRoot, trackName, BranchAnalysisWriter.BranchFileName);
    }
}
