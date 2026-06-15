namespace EternalLoop.AnalysisEngine.Cli;

public static class AnalysisEngineHelpWriter
{
    public static string GetHelpText()
    {
        return """
        EternalLoop Analysis Engine

        Usage:
          EternalLoop.AnalysisEngine.Cli --input <path> --output-dir <path> [options]

        Required:
          --input <path>          Full path to the audio file
          --output-dir <path>     Directory where analysis JSON files will be exported

        Options:
          --track-id <id>         Track id used by EternalLoop
          --title <title>         Track title used in exported metadata
          --artist <artist>       Artist name used in exported metadata
          --format <format>       raw, loop, or both
          --pretty                Writes indented JSON, enabled by default
          --force                 Overwrites existing output files
          --quiet                 Reduces progress output
          --musical-quality       Enables experimental musical quality fronts
          --mq-segmentation       Enables acoustic novelty segmentation only
          --mq-beat-microsnap     Enables beat micro-snap only
          --mq-tatums             Enables adaptive tatums only
          --mq-sections           Enables structural sections only
          --mq-confidences        Enables evidence-based confidences only
          --help                  Shows this help message

        Defaults:
          --format both
          --pretty true
          --track-id normalized input file name
          --title input file name without extension
          --artist Local
        """;
    }

    public static void Write()
    {
        Console.WriteLine(GetHelpText());
    }
}
