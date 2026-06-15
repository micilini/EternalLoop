using EternalLoop.Core.Workflow;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Workflow;

public sealed class TrackWorkflowServiceOptionsTests
{
    [Fact]
    public void DefaultShouldUseTempWorkspace()
    {
        TrackWorkflowServiceOptions.Default.WorkspaceRoot
            .Should()
            .Contain("EternalLoop");
    }

    [Fact]
    public void DefaultShouldForceIntermediateExports()
    {
        TrackWorkflowServiceOptions.Default.ForceIntermediateExports.Should().BeTrue();
    }

    [Fact]
    public void DefaultShouldUseBalancedTuning()
    {
        TrackWorkflowServiceOptions.Default.Tuning.Preset.Should().Be("Balanced");
        TrackWorkflowServiceOptions.Default.Tuning.MaxBranchesPerBeat.Should().Be(6);
        TrackWorkflowServiceOptions.Default.Tuning.BranchMaxThreshold.Should().Be(80);
    }

    [Fact]
    public void DefaultShouldUseCurrentSettingsSchemaVersion()
    {
        TrackWorkflowServiceOptions.Default.SettingsSchemaVersion.Should().Be(5);
    }
}
