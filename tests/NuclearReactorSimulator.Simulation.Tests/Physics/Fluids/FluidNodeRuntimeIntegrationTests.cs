using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class FluidNodeRuntimeIntegrationTests
{
    [Fact]
    public void DepletionFailure_IsRejectedTransactionallyByRuntime()
    {
        var runtime = CreateRuntime();
        runtime.EnqueueCommand(new SetBalanceCommand(new FluidNodeBalance(
            MassFlowRate.FromKilogramsPerSecond(-50_000d),
            Power.Zero)));

        Assert.Throws<SimulationRuntimeFaultException>(() => runtime.StepOnce());

        var snapshot = runtime.GetSnapshot();
        Assert.Equal(SimulationRunState.Faulted, snapshot.Runtime.RunState);
        Assert.Equal(0L, snapshot.Runtime.StepIndex);
        Assert.Equal(1, snapshot.Runtime.PendingCommandCount);
        Assert.Equal(1_000d, snapshot.State.MassKilograms, 12);
        Assert.Equal(100d, snapshot.State.InternalEnergyMegajoules, 12);
        Assert.NotNull(snapshot.Runtime.Fault);
        Assert.Contains(nameof(FluidNodeDepletionException), snapshot.Runtime.Fault!.ExceptionType);
    }

    [Fact]
    public void FluidNodeModel_ComposesWithDeterministicFixedStepRuntime()
    {
        var left = CreateRuntime();
        var right = CreateRuntime();

        left.EnqueueCommand(new SetBalanceCommand(new FluidNodeBalance(
            MassFlowRate.FromKilogramsPerSecond(10d),
            Power.FromMegawatts(2d))));
        right.EnqueueCommand(new SetBalanceCommand(new FluidNodeBalance(
            MassFlowRate.FromKilogramsPerSecond(10d),
            Power.FromMegawatts(2d))));

        left.Resume();
        right.Resume();

        left.Advance(TimeSpan.FromSeconds(2d));

        foreach (var pulse in new[] { 17, 83, 400, 7, 293, 1_200 })
        {
            right.Advance(TimeSpan.FromMilliseconds(pulse));
        }

        var leftSnapshot = left.GetSnapshot();
        var rightSnapshot = right.GetSnapshot();

        Assert.Equal(leftSnapshot, rightSnapshot);
        Assert.Equal(1_020d, leftSnapshot.State.MassKilograms, 10);
        Assert.Equal(104d, leftSnapshot.State.InternalEnergyMegajoules, 10);
        Assert.Equal(100L, leftSnapshot.Runtime.StepIndex);
    }

    private static SimulationRuntime<PlantState, SetBalanceCommand, PlantSnapshot> CreateRuntime()
    {
        var node = new FluidNodeState(
            new FluidNodeDefinition("primary-test-node", Volume.FromCubicMetres(2d)),
            new FluidNodeInventory(
                Mass.FromKilograms(1_000d),
                Energy.FromMegajoules(100d)),
            new FluidThermodynamicState(
                Pressure.FromMegapascals(5d),
                Temperature.FromDegreesCelsius(250d)));

        return new SimulationRuntime<PlantState, SetBalanceCommand, PlantSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            new PlantState(node, FluidNodeBalance.Zero),
            new PlantKernel());
    }

    private sealed record SetBalanceCommand(FluidNodeBalance Balance);

    private sealed record PlantState(FluidNodeState Node, FluidNodeBalance Balance);

    private sealed record PlantSnapshot(double MassKilograms, double InternalEnergyMegajoules);

    private sealed class PlantKernel : ISimulationKernel<PlantState, SetBalanceCommand, PlantSnapshot>
    {
        private readonly FluidNodeIntegrator _integrator = new(new StableThermodynamicModel());

        public PlantState Step(
            PlantState state,
            IReadOnlyList<QueuedSimulationCommand<SetBalanceCommand>> commands,
            SimulationStepContext context)
        {
            var balance = state.Balance;
            foreach (var command in commands)
            {
                balance = command.Command.Balance;
            }

            return new PlantState(
                _integrator.Step(state.Node, balance, context.DeltaTime),
                balance);
        }

        public PlantSnapshot CreateSnapshot(PlantState state)
        {
            return new PlantSnapshot(
                state.Node.Mass.Kilograms,
                state.Node.InternalEnergy.Megajoules);
        }
    }

    private sealed class StableThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
        {
            return previousState;
        }
    }
}
