namespace EternalLoop.AnalysisEngine.Cli;

public sealed class AnalysisEngineParseResult
{
    private AnalysisEngineParseResult()
    {
    }

    public bool IsSuccess { get; private init; }

    public bool IsHelp { get; private init; }

    public string? ErrorMessage { get; private init; }

    public AnalysisEngineArguments? Arguments { get; private init; }

    public static AnalysisEngineParseResult Success(AnalysisEngineArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        return new AnalysisEngineParseResult
        {
            IsSuccess = true,
            Arguments = arguments
        };
    }

    public static AnalysisEngineParseResult Help()
    {
        return new AnalysisEngineParseResult
        {
            IsHelp = true
        };
    }

    public static AnalysisEngineParseResult Error(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new AnalysisEngineParseResult
        {
            ErrorMessage = message
        };
    }
}
