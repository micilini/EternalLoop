using EternalLoop.BranchAnalysis.Cli.Cli;
using EternalLoop.BranchAnalysis.Core.Runner;

namespace EternalLoop.BranchAnalysis.Cli;

public static class AnalysisBranchCommand
{
    public static int Run(string[] args)
    {
        return Run(args, Console.Out, Console.Error);
    }

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        BranchAnalysisParseResult result = BranchAnalysisParser.Parse(args);

        if (result.Help)
        {
            BranchAnalysisHelpWriter.WriteHelp(output);
            return BranchAnalysisExitCodes.Success;
        }

        if (!result.IsValid)
        {
            foreach (string message in result.Errors)
            {
                error.WriteLine(message);
            }

            error.WriteLine();
            BranchAnalysisHelpWriter.WriteHelp(error);
            return BranchAnalysisExitCodes.InvalidArguments;
        }

        return BranchAnalysisRunner.Run(result.Options, output, error);
    }
}
