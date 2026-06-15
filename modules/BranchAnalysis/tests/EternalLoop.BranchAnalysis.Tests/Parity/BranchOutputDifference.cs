namespace EternalLoop.BranchAnalysis.Tests.Parity;

public sealed class BranchOutputDifference
{
    public string Path { get; init; } = string.Empty;
    public string NodeValue { get; init; } = string.Empty;
    public string CSharpValue { get; init; } = string.Empty;
    public string Message => $"{Path} differs. Node={NodeValue} CSharp={CSharpValue}";
}
