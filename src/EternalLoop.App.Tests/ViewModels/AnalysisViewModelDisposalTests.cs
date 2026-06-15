using EternalLoop.App.ViewModels;
using EternalLoop.Core.Workflow;
using FluentAssertions;

namespace EternalLoop.App.Tests.ViewModels;

public sealed class AnalysisViewModelDisposalTests
{
    [Fact]
    public async Task Dispose_cancels_running_workflow()
    {
        var workflow = new BlockingWorkflowService();
        var viewModel = new AnalysisViewModel("track.wav", workflow, () => { });

        Task startTask = viewModel.StartAsync();
        await workflow.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        viewModel.Dispose();
        await startTask.WaitAsync(TimeSpan.FromSeconds(5));

        workflow.TokenWasCanceled.Should().BeTrue();
        viewModel.IsRunning.Should().BeFalse();
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
