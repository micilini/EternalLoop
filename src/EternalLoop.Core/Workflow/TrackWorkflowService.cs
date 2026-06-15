using EternalLoop.AnalysisEngine.Core.Application;
using EternalLoop.AnalysisEngine.Core.Export.LoopAnalysis;
using EternalLoop.AnalysisEngine.Core.Options;
using EternalLoop.BranchAnalysis.Core.Application;
using EternalLoop.BranchAnalysis.Core.Export;
using EternalLoop.BranchAnalysis.Core.Runner;
using EternalLoop.Core.Cache;
using EternalLoop.Core.Diagnostics;
using EternalLoop.Core.Runtime;
using EternalLoop.Core.Settings;
using System.Text.Json;
using BranchApplicationResult = EternalLoop.BranchAnalysis.Core.Application.BranchAnalysisResult;

namespace EternalLoop.Core.Workflow;

public sealed class TrackWorkflowService : ITrackWorkflowService
{
    private readonly IAnalysisEngineService _analysisEngineService;
    private readonly IBranchAnalysisService _branchAnalysisService;
    private readonly LoopAnalysisJsonExporter _eternalLoopAnalysisExporter;
    private readonly TrackRuntimePackageBuilder _runtimePackageBuilder;
    private readonly TrackWorkflowServiceOptions _options;

    public TrackWorkflowService(
        IAnalysisEngineService analysisEngineService,
        IBranchAnalysisService branchAnalysisService,
        TrackWorkflowServiceOptions? options = null)
    {
        _analysisEngineService = analysisEngineService
            ?? throw new ArgumentNullException(nameof(analysisEngineService));
        _branchAnalysisService = branchAnalysisService
            ?? throw new ArgumentNullException(nameof(branchAnalysisService));
        _eternalLoopAnalysisExporter = new LoopAnalysisJsonExporter();
        _runtimePackageBuilder = new TrackRuntimePackageBuilder();
        _options = options ?? TrackWorkflowServiceOptions.Default;
    }

    public async Task<TrackWorkflowResult> RunAsync(
        TrackWorkflowRequest request,
        ITrackWorkflowProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ReportAsync(
                progressReporter,
                new TrackWorkflowProgress(
                    TrackWorkflowStatus.Queued,
                    "Track workflow queued.",
                    0),
                cancellationToken).ConfigureAwait(false);

            await ReportAsync(
                progressReporter,
                new TrackWorkflowProgress(
                    TrackWorkflowStatus.ValidatingInput,
                    "Validating track input.",
                    5),
                cancellationToken).ConfigureAwait(false);

            TrackWorkflowError? inputError = TrackWorkflowInputValidator.Validate(request.Input);

            if (inputError is not null)
            {
                _options.Logger?.Log(AppLogLevel.Warning, $"Track workflow input rejected: {inputError.Code}. {inputError.Details}");
                return TrackWorkflowResult.Failed(request.Input, inputError);
            }

            LoopTuningSettings tuning = _options.Tuning ?? LoopTuningSettings.Balanced();
            TrackFileIdentity? identity = null;

            if (_options.FileIdentityService is not null)
            {
                identity = await _options.FileIdentityService
                    .CreateAsync(request.Input.FilePath, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (!request.ForceReanalysis
                && identity is not null
                && _options.RuntimePackageCacheService is not null)
            {
                await ReportAsync(
                    progressReporter,
                    new TrackWorkflowProgress(
                        TrackWorkflowStatus.ValidatingInput,
                        "Checking runtime cache.",
                        8),
                    cancellationToken).ConfigureAwait(false);

                TrackRuntimePackageCacheResult cached = await _options.RuntimePackageCacheService
                    .TryLoadExactRuntimeAsync(
                        request.Input,
                        identity,
                        tuning,
                        _options.SettingsSchemaVersion,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (cached.IsHit
                    && cached.RuntimePackage is not null
                    && cached.AnalysisSummary is not null
                    && cached.BranchSummary is not null)
                {
                    await ReportAsync(
                        progressReporter,
                        new TrackWorkflowProgress(
                            TrackWorkflowStatus.Completed,
                            "Runtime package loaded from cache.",
                            100),
                        cancellationToken).ConfigureAwait(false);

                    return TrackWorkflowResult.Completed(
                        request.Input,
                        cached.AnalysisSummary,
                        cached.BranchSummary,
                        cached.RuntimePackage,
                        cacheHit: true,
                        analysisSource: "Cached analysis");
                }

                TrackRuntimePackageCacheResult compatibleBranch = await _options.RuntimePackageCacheService
                    .TryLoadCompatibleBranchAsync(
                        request.Input,
                        identity,
                        tuning,
                        _options.SettingsSchemaVersion,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (compatibleBranch.IsHit
                    && compatibleBranch.RuntimePackage is not null
                    && compatibleBranch.AnalysisSummary is not null
                    && compatibleBranch.BranchSummary is not null)
                {
                    await ReportAsync(
                        progressReporter,
                        new TrackWorkflowProgress(
                            TrackWorkflowStatus.PreparingRuntime,
                            "Rebuilding runtime tuning from cached branches.",
                            90),
                        cancellationToken).ConfigureAwait(false);

                    TrackRuntimePackage rebuiltRuntimePackage = compatibleBranch.RuntimePackage with
                    {
                        Files = compatibleBranch.RuntimePackage.Files with
                        {
                            RunRoot = CreateRunRoot(request)
                        }
                    };

                    await _options.RuntimePackageCacheService
                        .SaveAsync(
                            request.Input,
                            identity,
                            tuning,
                            _options.SettingsSchemaVersion,
                            rebuiltRuntimePackage,
                            cancellationToken)
                        .ConfigureAwait(false);

                    await ReportAsync(
                        progressReporter,
                        new TrackWorkflowProgress(
                            TrackWorkflowStatus.Completed,
                            "Runtime tuning rebuilt from cached branches.",
                            100),
                        cancellationToken).ConfigureAwait(false);

                    return TrackWorkflowResult.Completed(
                        request.Input,
                        compatibleBranch.AnalysisSummary,
                        compatibleBranch.BranchSummary,
                        rebuiltRuntimePackage,
                        cacheHit: true,
                        analysisSource: "Cached branches, rebuilt runtime tuning");
                }
            }

            string runRoot = CreateRunRoot(request);
            string analysisOutputDirectory = Path.Combine(runRoot, "analysis");
            string branchOutputRoot = Path.Combine(runRoot, "branches");

            Directory.CreateDirectory(analysisOutputDirectory);
            Directory.CreateDirectory(branchOutputRoot);

            AnalysisOptions analysisOptions = LoopTuningOptionsMapper.ToAnalysisOptions(tuning);

            await ReportAsync(
                progressReporter,
                new TrackWorkflowProgress(
                    TrackWorkflowStatus.AnalyzingAudio,
                    "Analyzing audio.",
                    10),
                cancellationToken).ConfigureAwait(false);

            AnalysisEngineResult analysisResult = await _analysisEngineService
                .AnalyzeAsync(
                    new AnalysisEngineRequest(request.Input.FilePath, analysisOptions),
                    new TrackWorkflowAnalysisProgressReporterAdapter(progressReporter),
                    cancellationToken)
                .ConfigureAwait(false);

            TrackAnalysisSummary analysisSummary = MapAnalysisSummary(analysisResult);

            var exportResult = await _eternalLoopAnalysisExporter
                .ExportAsync(
                    analysisResult.Analysis,
                    analysisOutputDirectory,
                    trackId: Path.GetFileNameWithoutExtension(request.Input.FileName),
                    title: Path.GetFileNameWithoutExtension(request.Input.FileName),
                    artist: AnalysisOptions.DefaultArtist,
                    force: _options.ForceIntermediateExports,
                    pretty: _options.PrettyIntermediateExports,
                    cancellationToken)
                .ConfigureAwait(false);

            await ReportAsync(
                progressReporter,
                new TrackWorkflowProgress(
                    TrackWorkflowStatus.BuildingBranches,
                    "Building branch graph.",
                    70),
                cancellationToken).ConfigureAwait(false);

            BranchAnalysisOptions branchOptions = LoopTuningOptionsMapper.ToBranchAnalysisOptions(
                tuning,
                force: _options.ForceIntermediateExports,
                pretty: _options.PrettyIntermediateExports,
                quiet: true);

            BranchApplicationResult branchResult = await _branchAnalysisService
                .AnalyzeAsync(
                    new BranchAnalysisRequest(
                        exportResult.FilePath,
                        branchOutputRoot,
                        analysisName: Path.GetFileNameWithoutExtension(request.Input.FileName),
                        options: branchOptions),
                    cancellationToken)
                .ConfigureAwait(false);

            TrackBranchSummary branchSummary = MapBranchSummary(branchResult);

            await ReportAsync(
                progressReporter,
                new TrackWorkflowProgress(
                    TrackWorkflowStatus.PreparingRuntime,
                    "Preparing runtime package.",
                    90),
                cancellationToken).ConfigureAwait(false);

            BranchExportPayload branchPayload = await ReadBranchPayloadAsync(
                branchResult.ItemResult.OutputPath,
                cancellationToken).ConfigureAwait(false);

            TrackRuntimePackage runtimePackage = _runtimePackageBuilder.Build(
                new TrackRuntimePackageBuildRequest
                {
                    Input = request.Input,
                    Analysis = analysisResult.Analysis,
                    RunRoot = runRoot,
                    AnalysisJsonPath = exportResult.FilePath,
                    BranchesJsonPath = branchResult.ItemResult.OutputPath,
                    BranchPayload = branchPayload,
                    Tuning = tuning,
                    SettingsSchemaVersion = _options.SettingsSchemaVersion
                });

            if (identity is not null && _options.RuntimePackageCacheService is not null)
            {
                await _options.RuntimePackageCacheService
                    .SaveAsync(
                        request.Input,
                        identity,
                        tuning,
                        _options.SettingsSchemaVersion,
                        runtimePackage,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            await ReportAsync(
                progressReporter,
                new TrackWorkflowProgress(
                    TrackWorkflowStatus.Completed,
                    "Track workflow completed.",
                    100),
                cancellationToken).ConfigureAwait(false);

            return TrackWorkflowResult.Completed(
                request.Input,
                analysisSummary,
                branchSummary,
                runtimePackage,
                cacheHit: false,
                analysisSource: "Fresh analysis");
        }
        catch (OperationCanceledException)
        {
            return TrackWorkflowResult.Canceled(request.Input);
        }
        catch (Exception exception)
        {
            TrackWorkflowError error = TrackWorkflowExceptionMapper.Map(exception);
            _options.Logger?.Log(
                AppLogLevel.Error,
                $"Track workflow failed: {error.Code}.",
                exception);
            return TrackWorkflowResult.Failed(
                request.Input,
                error);
        }
    }

    private string CreateRunRoot(TrackWorkflowRequest request)
    {
        string safeCorrelationId = SanitizePathSegment(request.CorrelationId);

        return Path.Combine(
            Path.GetFullPath(_options.WorkspaceRoot),
            safeCorrelationId);
    }

    private static string SanitizePathSegment(string value)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();

        var safeChars = value.Select(character =>
            invalidChars.Contains(character) ? '_' : character);

        string safeValue = new(safeChars.ToArray());

        return string.IsNullOrWhiteSpace(safeValue)
            ? Guid.NewGuid().ToString("N")
            : safeValue;
    }

    private static TrackAnalysisSummary MapAnalysisSummary(
        AnalysisEngineResult analysisResult)
    {
        return new TrackAnalysisSummary(
            analysisResult.Summary.Duration,
            analysisResult.Summary.BeatCount,
            analysisResult.Summary.SegmentCount,
            analysisResult.Summary.SectionCount);
    }

    private static TrackBranchSummary MapBranchSummary(
        BranchApplicationResult branchResult)
    {
        return new TrackBranchSummary(
            branchResult.Summary.ActiveBranches,
            branchResult.Summary.CandidateBranches);
    }

    private static async Task<BranchExportPayload> ReadBranchPayloadAsync(
        string branchesJsonPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(branchesJsonPath)
            || !File.Exists(branchesJsonPath))
        {
            throw new RuntimePackageBuildException("BranchAnalysis did not produce a branch export file.");
        }

        try
        {
            await using FileStream stream = File.OpenRead(branchesJsonPath);

            BranchExportPayload? payload = await JsonSerializer.DeserializeAsync<BranchExportPayload>(
                stream,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return payload ?? throw new RuntimePackageBuildException("BranchAnalysis branch export payload is empty.");
        }
        catch (JsonException exception)
        {
            throw new RuntimePackageBuildException("BranchAnalysis branch export payload is invalid.", exception);
        }
    }

    private static async ValueTask ReportAsync(
        ITrackWorkflowProgressReporter? progressReporter,
        TrackWorkflowProgress progress,
        CancellationToken cancellationToken)
    {
        if (progressReporter is null)
        {
            return;
        }

        await progressReporter
            .ReportAsync(progress, cancellationToken)
            .ConfigureAwait(false);
    }
}
