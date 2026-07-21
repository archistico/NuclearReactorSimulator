using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Simulation.Physics.Reactor;
using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor;

public sealed class ReactivityRuntimeIntegrationTests
{
    [Fact]
    public void ReactivityComposition_IsIndependentFromExternalPulseSegmentation()
    {
        var singlePulse = CreateRuntime();
        var irregularPulses = CreateRuntime();

        EnqueueSameCommands(singlePulse);
        EnqueueSameCommands(irregularPulses);
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
        Assert.Equal(125d, left.State.TotalPcm, 10);
        Assert.Equal(400d, left.State.ControlRodsPcm, 10);
        Assert.Equal(125d, left.State.VoidPcm, 10);
        Assert.Equal(-250d, left.State.XenonPcm, 10);
        Assert.Equal(-150d, left.State.FuelTemperaturePcm, 10);
    }

    private static SimulationRuntime<PlantState, SetContributionCommand, PlantSnapshot> CreateRuntime()
    {
        return new SimulationRuntime<PlantState, SetContributionCommand, PlantSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            new PlantState(
                Reactivity.Zero,
                Reactivity.Zero,
                Reactivity.Zero,
                Reactivity.Zero),
            new PlantKernel());
    }

    private static void EnqueueSameCommands(
        SimulationRuntime<PlantState, SetContributionCommand, PlantSnapshot> runtime)
    {
        runtime.EnqueueCommand(new SetContributionCommand(
            ReactivityContributionKind.ControlRods,
            Reactivity.FromPcm(400d)));
        runtime.EnqueueCommand(new SetContributionCommand(
            ReactivityContributionKind.Void,
            Reactivity.FromPcm(125d)));
        runtime.EnqueueCommand(new SetContributionCommand(
            ReactivityContributionKind.Xenon,
            Reactivity.FromPcm(-250d)));
        runtime.EnqueueCommand(new SetContributionCommand(
            ReactivityContributionKind.FuelTemperature,
            Reactivity.FromPcm(-150d)));
    }

    private sealed record SetContributionCommand(
        ReactivityContributionKind Kind,
        Reactivity Value);

    private sealed record PlantState(
        Reactivity ControlRods,
        Reactivity Void,
        Reactivity Xenon,
        Reactivity FuelTemperature);

    private sealed record PlantSnapshot(
        double TotalPcm,
        double ControlRodsPcm,
        double VoidPcm,
        double XenonPcm,
        double FuelTemperaturePcm);

    private sealed class PlantKernel : ISimulationKernel<PlantState, SetContributionCommand, PlantSnapshot>
    {
        private readonly ReactivityModel _model = new();

        public PlantState Step(
            PlantState state,
            IReadOnlyList<QueuedSimulationCommand<SetContributionCommand>> commands,
            SimulationStepContext context)
        {
            _ = context;

            foreach (var queued in commands)
            {
                state = queued.Command.Kind switch
                {
                    ReactivityContributionKind.ControlRods => state with { ControlRods = queued.Command.Value },
                    ReactivityContributionKind.Void => state with { Void = queued.Command.Value },
                    ReactivityContributionKind.Xenon => state with { Xenon = queued.Command.Value },
                    ReactivityContributionKind.FuelTemperature => state with { FuelTemperature = queued.Command.Value },
                    _ => state,
                };
            }

            return state;
        }

        public PlantSnapshot CreateSnapshot(PlantState state)
        {
            var breakdown = _model.Evaluate(
            [
                new ReactivityContribution("control-rods", ReactivityContributionKind.ControlRods, state.ControlRods),
                new ReactivityContribution("fuel-temperature", ReactivityContributionKind.FuelTemperature, state.FuelTemperature),
                new ReactivityContribution("void", ReactivityContributionKind.Void, state.Void),
                new ReactivityContribution("xenon", ReactivityContributionKind.Xenon, state.Xenon),
            ]);

            return new PlantSnapshot(
                breakdown.Total.Pcm,
                breakdown.TotalFor(ReactivityContributionKind.ControlRods).Pcm,
                breakdown.TotalFor(ReactivityContributionKind.Void).Pcm,
                breakdown.TotalFor(ReactivityContributionKind.Xenon).Pcm,
                breakdown.TotalFor(ReactivityContributionKind.FuelTemperature).Pcm);
        }
    }
}
