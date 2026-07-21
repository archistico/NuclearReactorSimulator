using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Simulation.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.ControlRods;

public sealed class ControlRodRuntimeIntegrationTests
{
    [Fact]
    public void RodMotionAndWorth_AreIndependentFromExternalPulseSegmentation()
    {
        var singlePulse = CreateRuntime();
        var irregularPulses = CreateRuntime();
        var command = new ControlRodCommand("bank-a", ControlRodCommandTargetKind.Group, ControlRodMotion.Withdraw);

        singlePulse.EnqueueCommand(command);
        irregularPulses.EnqueueCommand(command);
        singlePulse.Resume();
        irregularPulses.Resume();

        singlePulse.Advance(TimeSpan.FromSeconds(2d));
        foreach (var milliseconds in new[] { 17, 83, 400, 7, 293, 1_200 })
        {
            irregularPulses.Advance(TimeSpan.FromMilliseconds(milliseconds));
        }

        var left = singlePulse.GetSnapshot();
        var right = irregularPulses.GetSnapshot();

        Assert.Equal(left, right);
        Assert.Equal(100L, left.Runtime.StepIndex);
        Assert.Equal(50d, left.State.RodA1Percent, 8);
        Assert.Equal(50d, left.State.RodA2Percent, 8);
        Assert.Equal(-1_000d, left.State.TotalRodReactivityPcm, 8);
    }

    private static SimulationRuntime<ControlRodSystemState, ControlRodCommand, PlantSnapshot> CreateRuntime()
    {
        var definition = Definition();
        return new SimulationRuntime<ControlRodSystemState, ControlRodCommand, PlantSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            new ControlRodSystemState(
            [
                new ControlRodState("rod-a1", ControlRodPosition.FullyInserted),
                new ControlRodState("rod-a2", ControlRodPosition.FullyInserted),
            ]),
            new PlantKernel(definition));
    }

    private static ControlRodSystemDefinition Definition()
    {
        var rate = ControlRodTravelRate.FromFractionPerSecond(0.25d);
        return new ControlRodSystemDefinition(
        [
            new ControlRodDefinition("rod-a1", "bank-a", rate, Reactivity.FromPcm(-1_000d), Reactivity.Zero, ControlRodWorthCurveKind.Linear),
            new ControlRodDefinition("rod-a2", "bank-a", rate, Reactivity.FromPcm(-1_000d), Reactivity.Zero, ControlRodWorthCurveKind.Linear),
        ],
        [
            new ControlRodGroupDefinition("bank-a", ["rod-a1", "rod-a2"]),
        ]);
    }

    private sealed record PlantSnapshot(double RodA1Percent, double RodA2Percent, double TotalRodReactivityPcm);

    private sealed class PlantKernel : ISimulationKernel<ControlRodSystemState, ControlRodCommand, PlantSnapshot>
    {
        private readonly ControlRodSystemSolver _systemSolver;
        private readonly ControlRodReactivitySolver _reactivitySolver;

        public PlantKernel(ControlRodSystemDefinition definition)
        {
            _systemSolver = new ControlRodSystemSolver(definition);
            _reactivitySolver = new ControlRodReactivitySolver(definition);
        }

        public ControlRodSystemState Step(
            ControlRodSystemState state,
            IReadOnlyList<QueuedSimulationCommand<ControlRodCommand>> commands,
            SimulationStepContext context)
        {
            return _systemSolver.Step(
                state,
                commands.Select(static queued => queued.Command),
                context.DeltaTime);
        }

        public PlantSnapshot CreateSnapshot(ControlRodSystemState state)
        {
            var reactivity = _reactivitySolver.Evaluate(state);
            return new PlantSnapshot(
                state.GetRod("rod-a1").Position.PercentWithdrawn,
                state.GetRod("rod-a2").Position.PercentWithdrawn,
                reactivity.Total.Pcm);
        }
    }
}
