using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.Neutronics;

public sealed class PointKineticsRuntimeIntegrationTests
{
    [Fact]
    public void PointKinetics_IsIndependentFromExternalPulseSegmentation()
    {
        var singlePulse = CreateRuntime();
        var irregularPulses = CreateRuntime();

        singlePulse.EnqueueCommand(new SetReactivityCommand(Reactivity.FromPcm(100d)));
        irregularPulses.EnqueueCommand(new SetReactivityCommand(Reactivity.FromPcm(100d)));
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
        Assert.Equal(100d, left.State.ReactivityPcm, 10);
        Assert.True(left.State.NeutronPopulation > 1d);
        Assert.True(left.State.ReactorPeriodSeconds > 0d);
    }

    private static SimulationRuntime<PlantState, SetReactivityCommand, PlantSnapshot> CreateRuntime()
    {
        var parameters = Parameters();
        return new SimulationRuntime<PlantState, SetReactivityCommand, PlantSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            new PlantState(
                PointKineticsState.CreateCriticalEquilibrium(parameters, NeutronPopulation.Reference),
                Reactivity.Zero),
            new PlantKernel(parameters));
    }

    private static PointKineticsParameters Parameters()
        => new(
            TimeSpan.FromMilliseconds(5d),
            [
                new DelayedNeutronGroupDefinition(
                    "slow",
                    DelayedNeutronFraction.FromFraction(0.004d),
                    DecayConstant.FromPerSecond(0.08d)),
                new DelayedNeutronGroupDefinition(
                    "fast",
                    DelayedNeutronFraction.FromFraction(0.0025d),
                    DecayConstant.FromPerSecond(0.8d)),
            ]);

    private sealed record SetReactivityCommand(Reactivity Reactivity);

    private sealed record PlantState(PointKineticsState Kinetics, Reactivity Reactivity);

    private sealed record PlantSnapshot(
        double NeutronPopulation,
        double ReactivityPcm,
        double? ReactorPeriodSeconds,
        double FastPrecursor,
        double SlowPrecursor);

    private sealed class PlantKernel : ISimulationKernel<PlantState, SetReactivityCommand, PlantSnapshot>
    {
        private readonly PointKineticsSolver _solver;

        public PlantKernel(PointKineticsParameters parameters)
        {
            _solver = new PointKineticsSolver(parameters);
        }

        public PlantState Step(
            PlantState state,
            IReadOnlyList<QueuedSimulationCommand<SetReactivityCommand>> commands,
            SimulationStepContext context)
        {
            foreach (var queued in commands)
            {
                state = state with { Reactivity = queued.Command.Reactivity };
            }

            return state with
            {
                Kinetics = _solver.Step(state.Kinetics, state.Reactivity, context.DeltaTime),
            };
        }

        public PlantSnapshot CreateSnapshot(PlantState state)
        {
            var diagnostics = _solver.CreateSnapshot(state.Kinetics, state.Reactivity);
            return new PlantSnapshot(
                state.Kinetics.NeutronPopulation.Relative,
                state.Reactivity.Pcm,
                diagnostics.ReactorPeriodSeconds,
                state.Kinetics.GetGroup("fast").PrecursorPopulation.Relative,
                state.Kinetics.GetGroup("slow").PrecursorPopulation.Relative);
        }
    }
}
