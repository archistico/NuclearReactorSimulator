using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Criticality;
using NuclearReactorSimulator.Application.Scenarios.Xenon;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.ViewModels;

public sealed class M9ControlRoomIntegrationGateTests
{
    [Fact]
    public void RealXenonScenario_ViewModelTracksCoordinatorRunPauseAndSingleStepWithoutLosingCanonicalDiagnostics()
    {
        var session = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new XenonRestartInitialConditionFactory(),
            new LowPowerXenonInitialConditionFactory(),
        })).Load(AdvancedXenonScenarioPack.RestartAfterShutdown);
        var viewModel = new MainWindowViewModel(
            new ApplicationDescriptor("Nuclear Reactor Simulator", "M9.7 TEST", "integration gate"),
            session.SnapshotSource,
            session.CommandDispatcher);

        viewModel.SelectedWorkspace = viewModel.Workspaces.Single(static workspace => workspace.Id == ControlRoomWorkspaceId.Reactor);
        var initialStep = viewModel.LogicalStep;
        var initialXenon = viewModel.ReactorCore.XenonReactivity.NumericValue;

        Assert.True(viewModel.IsReactorWorkspaceSelected);
        Assert.True(initialXenon.HasValue);
        Assert.True(initialXenon.Value < 0d);
        Assert.True(viewModel.XenonAvailabilityText.Contains("promoted", StringComparison.OrdinalIgnoreCase));

        viewModel.RunCommand.Execute(null);
        Assert.Equal("RUNNING", viewModel.RuntimeState);
        viewModel.PauseCommand.Execute(null);
        Assert.Equal("PAUSED", viewModel.RuntimeState);
        viewModel.SingleStepCommand.Execute(null);

        Assert.Equal(initialStep + 1, viewModel.LogicalStep);
        Assert.True(viewModel.ReactorCore.XenonReactivity.NumericValue.HasValue);
        Assert.True(viewModel.ReactorCore.XenonReactivity.NumericValue.Value < 0d);
        Assert.Equal(session.Coordinator.Current.LogicalStep, viewModel.LogicalStep);
    }

    [Fact]
    public void LegacyM7Runtime_ViewModelKeepsUnavailableXenonExplicitInsteadOfFabricatingAValue()
    {
        var coordinator = new ControlRoomRuntimeCoordinator(
            new FirstCriticalityInitialConditionFactory().CreateRuntimeEngine());
        var viewModel = new MainWindowViewModel(
            new ApplicationDescriptor("Nuclear Reactor Simulator", "M9.7 TEST", "integration gate"),
            coordinator,
            coordinator);

        Assert.Equal(ControlRoomVisualState.Unavailable, viewModel.ReactorCore.XenonReactivity.State);
        Assert.Null(viewModel.ReactorCore.XenonReactivity.NumericValue);
        Assert.True(viewModel.XenonAvailabilityText.Contains("unavailable", StringComparison.OrdinalIgnoreCase));
    }
}
