namespace EternalLoop.BranchAnalysis.Core.Validation;

public sealed class AnalysisContractValidationException : Exception
{
    public AnalysisContractValidationException(string message)
        : base(message)
    {
    }
}
