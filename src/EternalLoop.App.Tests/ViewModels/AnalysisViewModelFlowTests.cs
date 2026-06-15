using EternalLoop.App.ViewModels;
using EternalLoop.Core.Workflow;
using FluentAssertions;

namespace EternalLoop.App.Tests.ViewModels;

public sealed class AnalysisViewModelFlowTests
{
    [Fact]
    public async Task SuccessfulWorkflowShouldInvokeCompletionOnce()
    {
        TrackWorkflowResult result = CompletedResult();
        var workflow = new ResultWorkflowService(result);
        int completionCount = 0;
        var viewModel = new AnalysisViewModel("track.wav", workflow, () => { }, completed: _ => completionCount++);

        await viewModel.StartAsync();

        completionCount.Should().Be(1);
        viewModel.CurrentStage.Should().Be(TrackWorkflowStatus.Completed.ToString());
        viewModel.OverallProgress.Should().Be(100);
    }

    [Fact]
    public async Task FailedWorkflowShouldShowFriendlyStatusAndNotInvokeCompletion()
    {
        var workflow = new ResultWorkflowService(TrackWorkflowResult.Failed(
            TrackInput.FromFilePath("track.wav"),
            new TrackWorkflowError("failed", "Analysis failed.")));
        bool completed = false;
        var viewModel = new AnalysisViewModel("track.wav", workflow, () => { }, completed: _ => completed = true);

        await viewModel.StartAsync();

        completed.Should().BeFalse();
        viewModel.ErrorMessage.Should().Be("Analysis failed.");
        viewModel.FriendlyProgressText.Should().Be("Review the selected file and try again.");
    }

    [Fact]
    public async Task WorkflowExceptionShouldBecomeFriendlyStatus()
    {
        var workflow = new ThrowingWorkflowService();
        var viewModel = new AnalysisViewModel("track.wav", workflow, () => { });

        await viewModel.StartAsync();

        viewModel.CurrentStage.Should().Be(TrackWorkflowStatus.Failed.ToString());
        viewModel.ErrorMessage.Should().Be("EternalLoop could not prepare this track. Check the file and try again.");
        viewModel.Log.Should().Contain("Workflow failed unexpectedly.");
    }

    [Fact]
    public async Task ForceReanalysisFlagShouldBePassedToWorkflow()
    {
        var workflow = new ResultWorkflowService(CompletedResult());
        var viewModel = new AnalysisViewModel("track.wav", workflow, () => { }, forceReanalysis: true);

        await viewModel.StartAsync();

        workflow.LastRequest.Should().NotBeNull();
        workflow.LastRequest!.ForceReanalysis.Should().BeTrue();
    }

    [Fact]
    public async Task CancelCommandShouldCancelRunningWorkflow()
    {
        var workflow = new BlockingWorkflowService();
        var viewModel = new AnalysisViewModel("track.wav", workflow, () => { });
        Task startTask = viewModel.StartAsync();
        await workflow.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        viewModel.CancelCommand.Execute(null);
        await startTask.WaitAsync(TimeSpan.FromSeconds(5));

        workflow.TokenWasCanceled.Should().BeTrue();
        viewModel.CurrentStage.Should().Be(TrackWorkflowStatus.Canceled.ToString());
    }

    [Fact]
    public async Task BackCommandShouldCancelRunningWorkflowAndInvokeBackCallback()
    {
        var workflow = new BlockingWorkflowService();
        bool backInvoked = false;
        var viewModel = new AnalysisViewModel("track.wav", workflow, () => backInvoked = true);
        Task startTask = viewModel.StartAsync();
        await workflow.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        viewModel.BackCommand.Execute(null);
        await startTask.WaitAsync(TimeSpan.FromSeconds(5));

        backInvoked.Should().BeTrue();
        workflow.TokenWasCanceled.Should().BeTrue();
    }

    private static TrackWorkflowResult CompletedResult()
    {
        return TrackWorkflowResult.Completed(
            TrackInput.FromFilePath("track.wav"),
            new TrackAnalysisSummary(TimeSpan.FromSeconds(2), 2, 0, 0),
            new TrackBranchSummary(1, 1),
            PlayerViewModelDisposalTests.CreatePackage());
    }

    private sealed class ResultWorkflowService(TrackWorkflowResult result) : ITrackWorkflowService
    {
        public TrackWorkflowRequest? LastRequest { get; private set; }

        public Task<TrackWorkflowResult> RunAsync(
            TrackWorkflowRequest request,
            ITrackWorkflowProgressReporter? progressReporter = null,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(result);
        }
    }

    private sealed class ThrowingWorkflowService : ITrackWorkflowService
    {
        public Task<TrackWorkflowResult> RunAsync(
            TrackWorkflowRequest request,
            ITrackWorkflowProgressReporter? progressReporter = null,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Workflow failed.");
        }
    }

    private sealed class BlockingWorkflowService : ITrackWorkflowService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool TokenWasCanceled { get; private set; }

        public async Task<TrackWorkflowResult> RunAsync(
            TrackWorkflowRequest request,
            ITrackWorkflowProgressReporter? progressReporter = null,
            CancellationToken cancellationToken = default)
        {
            Started.SetResult();

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException();
            }
            catch (OperationCanceledException)
            {
                TokenWasCanceled = true;
                throw;
            }
        }
    }
}
