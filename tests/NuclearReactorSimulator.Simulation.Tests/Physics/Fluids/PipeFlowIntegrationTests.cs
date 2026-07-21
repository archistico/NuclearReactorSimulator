using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class PipeFlowIntegrationTests
{
    [Fact]
    public void OnePipeStep_ConservesTotalMassAndInternalEnergy()
    {
        var from = CreateNode("from", 5.4d, 1_000d, 100d);
        var to = CreateNode("to", 5.0d, 800d, 120d);
        var solver = new PipeFlowSolver();
        var integrator = new FluidNodeIntegrator(new StableThermodynamicModel());
        var flow = solver.Solve(CreatePipe(), from, to);

        var nextFrom = integrator.Step(from, flow.FromNodeBalance, TimeSpan.FromSeconds(1d));
        var nextTo = integrator.Step(to, flow.ToNodeBalance, TimeSpan.FromSeconds(1d));

        Assert.Equal(1_800d, nextFrom.Mass.Kilograms + nextTo.Mass.Kilograms, 12);
        Assert.Equal(220d, nextFrom.InternalEnergy.Megajoules + nextTo.InternalEnergy.Megajoules, 12);
        Assert.Equal(998d, nextFrom.Mass.Kilograms, 12);
        Assert.Equal(802d, nextTo.Mass.Kilograms, 12);
        Assert.Equal(99.8d, nextFrom.InternalEnergy.Megajoules, 12);
        Assert.Equal(120.2d, nextTo.InternalEnergy.Megajoules, 12);
    }

    [Fact]
    public void PipeModel_ComposesWithDeterministicFixedStepRuntime()
    {
        var left = CreateRuntime();
        var right = CreateRuntime();

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
        Assert.Equal(100L, leftSnapshot.Runtime.StepIndex);
        Assert.Equal(1_800d, leftSnapshot.State.TotalMassKilograms, 10);
        Assert.Equal(220d, leftSnapshot.State.TotalInternalEnergyMegajoules, 10);
    }

    private static SimulationRuntime<PlantState, NoCommand, PlantSnapshot> CreateRuntime()
    {
        return new SimulationRuntime<PlantState, NoCommand, PlantSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            new PlantState(
                CreateNode("from", 5.4d, 1_000d, 100d),
                CreateNode("to", 5.0d, 800d, 120d)),
            new PlantKernel());
    }

    private static PipeDefinition CreatePipe()
    {
        return new PipeDefinition(
            "primary-pipe",
            "from",
            "to",
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000d));
    }

    private static FluidNodeState CreateNode(
        string id,
        double pressureMegapascals,
        double massKilograms,
        double internalEnergyMegajoules)
    {
        return new FluidNodeState(
            new FluidNodeDefinition(id, Volume.FromCubicMetres(2d)),
            new FluidNodeInventory(
                Mass.FromKilograms(massKilograms),
                Energy.FromMegajoules(internalEnergyMegajoules)),
            new FluidThermodynamicState(
                Pressure.FromMegapascals(pressureMegapascals),
                Temperature.FromDegreesCelsius(250d)));
    }

    private sealed record NoCommand();

    private sealed record PlantState(FluidNodeState FromNode, FluidNodeState ToNode);

    private sealed record PlantSnapshot(
        double FromMassKilograms,
        double ToMassKilograms,
        double TotalMassKilograms,
        double TotalInternalEnergyMegajoules);

    private sealed class PlantKernel : ISimulationKernel<PlantState, NoCommand, PlantSnapshot>
    {
        private readonly PipeFlowSolver _flowSolver = new();
        private readonly PipeDefinition _pipe = CreatePipe();
        private readonly FluidNodeIntegrator _integrator = new(new StableThermodynamicModel());

        public PlantState Step(
            PlantState state,
            IReadOnlyList<QueuedSimulationCommand<NoCommand>> commands,
            SimulationStepContext context)
        {
            _ = commands;
            var flow = _flowSolver.Solve(_pipe, state.FromNode, state.ToNode);

            return new PlantState(
                _integrator.Step(state.FromNode, flow.FromNodeBalance, context.DeltaTime),
                _integrator.Step(state.ToNode, flow.ToNodeBalance, context.DeltaTime));
        }

        public PlantSnapshot CreateSnapshot(PlantState state)
        {
            return new PlantSnapshot(
                state.FromNode.Mass.Kilograms,
                state.ToNode.Mass.Kilograms,
                state.FromNode.Mass.Kilograms + state.ToNode.Mass.Kilograms,
                state.FromNode.InternalEnergy.Megajoules + state.ToNode.InternalEnergy.Megajoules);
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
