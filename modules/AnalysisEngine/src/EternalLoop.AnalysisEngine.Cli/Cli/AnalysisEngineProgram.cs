using EternalLoop.AnalysisEngine.Cli.Composition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EternalLoop.AnalysisEngine.Cli;

public static class AnalysisEngineProgram
{
    public static int Run(string[] args)
    {
        return RunAsync(args, CancellationToken.None).GetAwaiter().GetResult();
    }

    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var parseResult = AnalysisEngineParser.Parse(args);

        if (parseResult.IsHelp)
        {
            AnalysisEngineHelpWriter.Write();
            return AnalysisEngineExitCodes.Success;
        }

        if (!parseResult.IsSuccess || parseResult.Arguments is null)
        {
            Console.Error.WriteLine(parseResult.ErrorMessage ?? "Invalid arguments.");
            Console.Error.WriteLine();
            AnalysisEngineHelpWriter.Write();
            return AnalysisEngineExitCodes.InvalidArguments;
        }

        await using var serviceProvider = CreateServiceProvider(parseResult.Arguments.Quiet);
        var command = serviceProvider.GetRequiredService<AnalysisEngineCommand>();

        return await command
            .ExecuteAsync(parseResult.Arguments, cancellationToken)
            .ConfigureAwait(false);
    }

    private static ServiceProvider CreateServiceProvider(bool quiet)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(quiet ? LogLevel.Warning : LogLevel.Information);

            if (!quiet)
            {
                builder.AddConsole();
            }
        });

        services.AddAnalysisEngineServices();

        return services.BuildServiceProvider();
    }
}
