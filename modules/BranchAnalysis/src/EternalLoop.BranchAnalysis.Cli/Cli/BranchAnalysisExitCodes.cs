namespace EternalLoop.BranchAnalysis.Cli.Cli;

public static class BranchAnalysisExitCodes
{
    public const int Success = 0;
    public const int InvalidArguments = 1;
    public const int AnalysisRootNotFound = 2;
    public const int NoAnalysisFilesFound = 3;
    public const int ValidationFailed = 4;
    public const int BranchAnalysisFailed = 5;
    public const int ExportFailed = 6;
}
