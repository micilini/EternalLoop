namespace EternalLoop.AnalysisEngine.Cli;

public static class AnalysisEngineExitCodes
{
    public const int Success = 0;
    public const int InvalidArguments = 1;
    public const int InputFileNotFound = 2;
    public const int OutputAlreadyExists = 3;
    public const int AudioLoadFailed = 4;
    public const int AnalysisFailed = 5;
    public const int ExportFailed = 6;
    public const int ValidationFailed = 7;
}