using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class PumpFlowIntegrationTests
{
    [Fact]
    public void OnePumpStep_ConservesMassAndAddsExactlyHydraulicWorkToFluidInventory()
    {
        var from = CreateNode("from", 5d, 1_000d, 100d);
        var to = CreateNode("to", 5d, 800d, 120d);
        var pump = CreatePump();
        var flow = new PumpFlowSolver().Solve(
            pump,
            new PumpState(pump.Id, PumpSpeed.Rated),
            from,
            to);
        var integrator = new FluidNodeIntegrator(new StableThermodynamicModel());

        var nextFrom = integrator.Step(from, flow.FromNodeBalance, TimeSpan.FromSeconds(1d));
        var nextTo = integrator.Step(to, flow.ToNodeBalance, TimeSpan.FromSeconds(1d));

        Assert.Equal(1_800d, nextFrom.Mass.Kilograms + nextTo.Mass.Kilograms, 12);
        Assert.Equal(998d, nextFrom.Mass.Kilograms, 12);
        Assert.Equal(802d, nextTo.Mass.Kilograms, 12);
        Assert.Equal(
            220d + (flow.HydraulicPowerExchange.Watts / 1_000_000d),
            nextFrom.InternalEnergy.Megajoules + nextTo.InternalEnergy.Megajoules,
            12);
    }

    [Fact]
    public void PumpModel_ComposesWithDeterministicFixedStepRuntime()
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
        Assert.True(leftSnapshot.State.TotalInternalEnergyMegajoules > 220d);
        Assert.True(leftSnapshot.State.LastShaftPowerWatts > 0d);
    }

    [Fact]
    public void StoppedPump_WithEqualPressuresPreservesInventoriesOverLongRun()
    {
        var runtime = new SimulationRuntime<PlantState, NoCommand, PlantSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            new PlantState(
                CreateNode("from", 5d, 1_000d, 100d),
                CreateNode("to", 5d, 800d, 120d),
                new PumpState("pump", PumpSpeed.Rated, false),
                0d),
            new PlantKernel());

        runtime.Resume();
        runtime.Advance(TimeSpan.FromSeconds(10d));

        var snapshot = runtime.GetSnapshot();

        Assert.Equal(1_000d, snapshot.State.FromMassKilograms, 12);
        Assert.Equal(800d, snapshot.State.ToMassKilograms, 12);
        Assert.Equal(220d, snapshot.State.TotalInternalEnergyMegajoules, 12);
        Assert.Equal(0d, snapshot.State.LastShaftPowerWatts, 12);
    }

    private static SimulationRuntime<PlantState, NoCommand, PlantSnapshot> CreateRuntime()
    {
        return new SimulationRuntime<PlantState, NoCommand, PlantSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            new PlantState(
                CreateNode("from", 5d, 1_000d, 100d),
                CreateNode("to", 5d, 800d, 120d),
                new PumpState("pump", PumpSpeed.FromPercent(75d)),
                0d),
            new PlantKernel());
    }

    private static PumpDefinition CreatePump()
    {
        return new PumpDefinition(
            "pump",
            new PipeDefinition(
                "pump-path",
                "from",
                "to",
                QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000d)),
            PressureDifference.FromMegapascals(0.4d),
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000d),
            PumpEfficiency.FromPercent(80d));
    }

    private static FluidNodeState CreateNode(
        string id,
        double pressureMegapascals,
        double massKilograms,
        double internalEnergyMegajoules)
    {
        return new FluidNodeState(
            new FluidNodeDefinition(id, Volume.FromCubicMetres(1d)),
            new FluidNodeInventory(
                Mass.FromKilograms(massKilograms),
                Energy.FromMegajoules(internalEnergyMegajoules)),
            new FluidThermodynamicState(
                Pressure.FromMegapascals(pressureMegapascals),
                Temperature.FromDegreesCelsius(250d)));
    }

    private sealed record NoCommand();

    private sealed record PlantState(
        FluidNodeState FromNode,
        FluidNodeState ToNode,
        PumpState PumpState,
        double LastShaftPowerWatts);

    private sealed record PlantSnapshot(
        double FromMassKilograms,
        double ToMassKilograms,
        double TotalMassKilograms,
        double TotalInternalEnergyMegajoules,
        double LastShaftPowerWatts);

    private sealed class PlantKernel : ISimulationKernel<PlantState, NoCommand, PlantSnapshot>
    {
        private readonly PumpFlowSolver _flowSolver = new();
        private readonly PumpDefinition _pump = CreatePump();
        private readonly FluidNodeIntegrator _integrator = new(new StableThermodynamicModel());

        public PlantState Step(
            PlantState state,
            IReadOnlyList<QueuedSimulationCommand<NoCommand>> commands,
            SimulationStepContext context)
        {
            _ = commands;
            var flow = _flowSolver.Solve(_pump, state.PumpState, state.FromNode, state.ToNode);

            return state with
            {
                FromNode = _integrator.Step(state.FromNode, flow.FromNodeBalance, context.DeltaTime),
                ToNode = _integrator.Step(state.ToNode, flow.ToNodeBalance, context.DeltaTime),
                LastShaftPowerWatts = flow.ShaftPowerDemand.Watts,
            };
        }

        public PlantSnapshot CreateSnapshot(PlantState state)
        {
            return new PlantSnapshot(
                state.FromNode.Mass.Kilograms,
                state.ToNode.Mass.Kilograms,
                state.FromNode.Mass.Kilograms + state.ToNode.Mass.Kilograms,
                state.FromNode.InternalEnergy.Megajoules + state.ToNode.InternalEnergy.Megajoules,
                state.LastShaftPowerWatts);
        }
    }

    private sealed class StableThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
        {
            _ = definition;
            _ = inventory;
            return previousState;
        }
    }
}
