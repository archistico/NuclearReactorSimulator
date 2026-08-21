using NuclearReactorSimulator.App.Runtime;
using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.Runtime;

public sealed class DesktopControlRoomRuntimePumpTests
{
    [Fact]
    public void RunPump_AdvancesFixedStepsWhileRunningAndStopsAdvancingWhenPaused()
    {
        var session = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationInitialConditionFactory(),
        })).Load(DesktopIntegratedOperationsProgram.Scenario);
        var viewModel = new MainWindowViewModel(
            new ApplicationDescriptor("Nuclear Reactor Simulator", "M9.7 TEST", "desktop runtime pump"),
            session.SnapshotSource,
            session.CommandDispatcher);
        string? failure = null;
        var pump = new DesktopControlRoomRuntimePump(session.Coordinator, message => failure = message);

        viewModel.RunCommand.Execute(null);
        var first = pump.Tick();
        var second = pump.Tick();

        Assert.Null(failure);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal((long)DesktopControlRoomRuntimePump.SimulationStepsPerTick * 2L, viewModel.LogicalStep);
        Assert.True(viewModel.IsRuntimeRunning);
        Assert.Equal($"STEP {viewModel.LogicalStep}", viewModel.RuntimeProgressText);

        viewModel.PauseCommand.Execute(null);
        var pausedAt = viewModel.LogicalStep;
        var pausedTick = pump.Tick();

        Assert.Null(pausedTick);
        Assert.Equal(pausedAt, viewModel.LogicalStep);
        Assert.False(viewModel.IsRuntimeRunning);
    }

    [Fact]
    public void RunPump_DefaultDesktopSessionAdvancesTenSimulatedSecondsWithoutHostFailure()
    {
        var session = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationInitialConditionFactory(),
        })).Load(DesktopIntegratedOperationsProgram.Scenario);
        var viewModel = new MainWindowViewModel(
            new ApplicationDescriptor("Nuclear Reactor Simulator", "M9.7 TEST", "desktop runtime pump endurance"),
            session.SnapshotSource,
            session.CommandDispatcher);
        string? failure = null;
        var pump = new DesktopControlRoomRuntimePump(session.Coordinator, message => failure = message);

        viewModel.RunCommand.Execute(null);
        var requiredTicks = 1_000 / DesktopControlRoomRuntimePump.SimulationStepsPerTick;
        for (var tick = 0; tick < requiredTicks; tick++)
        {
            if (pump.Tick() is null)
            {
                break;
            }
        }

        Assert.Null(failure);
        Assert.Equal(1_000, viewModel.LogicalStep);
        Assert.True(viewModel.IsRuntimeRunning);
    }

    [Fact]
    public void RunPump_ArithmeticFailurePausesReportsOnceAndPreventsRepeatedAdvanceStorm()
    {
        var running = true;
        var advanceCalls = 0;
        var pauseCalls = 0;
        var failures = new List<string>();
        var pump = new DesktopControlRoomRuntimePump(
            () => running,
            () =>
            {
                advanceCalls++;
                throw new ArithmeticException("non-finite hydraulic probe");
            },
            () =>
            {
                pauseCalls++;
                running = false;
            },
            failures.Add);

        Assert.Null(pump.Tick());
        Assert.False(running);
        Assert.Equal(1, advanceCalls);
        Assert.Equal(1, pauseCalls);
        Assert.Equal(new[] { "non-finite hydraulic probe" }, failures);

        Assert.Null(pump.Tick());
        Assert.Equal(1, advanceCalls);
        Assert.Equal(1, pauseCalls);
        Assert.Single(failures);
    }

    [Fact]
    public void RunPump_OverflowFailureUsesTheSameExpectedNumericalBoundary()
    {
        var running = true;
        var pauseCalls = 0;
        string? failure = null;
        var pump = new DesktopControlRoomRuntimePump(
            () => running,
            static () => throw new OverflowException("checked sequence overflow"),
            () =>
            {
                pauseCalls++;
                running = false;
            },
            message => failure = message);

        Assert.Null(pump.Tick());
        Assert.False(running);
        Assert.Equal(1, pauseCalls);
        Assert.Equal("checked sequence overflow", failure);
    }

    [Fact]
    public void RunPump_UnknownProgrammingFailureIsNotSilentlySwallowed()
    {
        var pump = new DesktopControlRoomRuntimePump(
            static () => true,
            static () => throw new NullReferenceException("programming defect"),
            static () => throw new InvalidOperationException("pause must not run"),
            static _ => throw new InvalidOperationException("report must not run"));

        var exception = Assert.Throws<NullReferenceException>(() => pump.Tick());
        Assert.Equal("programming defect", exception.Message);
    }
}
