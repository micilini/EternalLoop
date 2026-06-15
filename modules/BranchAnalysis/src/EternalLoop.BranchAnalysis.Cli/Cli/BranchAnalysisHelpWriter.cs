using EternalLoop.BranchAnalysis.Core.Config;

namespace EternalLoop.BranchAnalysis.Cli.Cli;

public static class BranchAnalysisHelpWriter
{
    public static string BuildHelpText()
    {
        string[] lines =
        [
            "EternalLoop Branch Analysis CLI",
            "",
            "Usage:",
            "  EternalLoop.BranchAnalysis.Cli.exe [options]",
            "",
            "Options:",
            "  --analysis-root <path>       Folder containing exported audio analysis folders",
            "  --output-root <path>         Folder where branch analysis files will be written",
            "  --quantum-type beats         Quantum type used for branch calculation",
            "  --similarity-threshold <0.65-0.95>  Higher = stricter branch distance gate",
            "  --lookahead-depth <number>          Future beat continuity gate depth",
            "  --min-jump-distance <beats>         Minimum beat distance for branch jumps",
            "  --max-branches <number>      Maximum candidate branches per quantum",
            "  --max-threshold <number>     Maximum branch distance threshold",
            "  --force                      Overwrite existing branch output files",
            "  --pretty                     Write formatted JSON",
            "  --quiet                      Reduce console output",
            "  --disable-structural-policy  Use legacy acoustic-only branch selection for A/B tests",
            "  --help                       Show this help",
            "",
            "Defaults:",
            $"  analysis-root = {BranchAnalysisDefaults.AnalysisRoot}",
            $"  output-root   = {BranchAnalysisDefaults.OutputRoot}",
            $"  quantum-type  = {BranchAnalysisDefaults.QuantumType}",
            $"  similarity-threshold = {BranchAnalysisDefaults.SimilarityThreshold:0.00}",
            $"  lookahead-depth      = {BranchAnalysisDefaults.LookaheadDepth}",
            $"  min-jump-distance    = {BranchAnalysisDefaults.MinJumpDistance}",
            $"  max-branches  = {BranchAnalysisDefaults.MaxBranches}",
            $"  max-threshold = {BranchAnalysisDefaults.MaxThreshold}",
            $"  structural    = {BranchAnalysisDefaults.StructuralPolicy}",
            $"  force         = {BranchAnalysisDefaults.Force}",
            $"  pretty        = {BranchAnalysisDefaults.Pretty}",
            "",
            "Runtime isolation:",
            "  Reference implementations are comparison-only and are not imported at runtime.",
            ""
        ];

        return string.Join(Environment.NewLine, lines);
    }

    public static void WriteHelp(TextWriter output)
    {
        output.Write(BuildHelpText());
    }
}
