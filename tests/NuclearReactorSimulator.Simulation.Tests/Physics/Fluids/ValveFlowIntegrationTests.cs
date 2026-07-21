using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class ValveFlowIntegrationTests
{
    [Fact]
    public void OneValveStep_ConservesTotalMassAndInternalEnergy()
    {
        var from = CreateNode("from", 5.4d, 1_000d, 100d);
        var to = CreateNode("to", 5.0d, 800d, 120d);
        var solver = new ValveFlowSolver();
        var integrator = new FluidNodeIntegrator(new StableThermodynamicModel());
        var valve = CreateValve();
        var flow = solver.Solve(
            valve,
            new ValveState(valve.Id, ValvePosition.FromPercent(50d)),
            from,
            to);

        var nextFrom = integrator.Step(from, flow.FromNodeBalance, TimeSpan.FromSeconds(1d));
        var nextTo = integrator.Step(to, flow.ToNodeBalance, TimeSpan.FromSeconds(1d));

        Assert.Equal(1_800d, nextFrom.Mass.Kilograms + nextTo.Mass.Kilograms, 12);
        Assert.Equal(220d, nextFrom.InternalEnergy.Megajoules + nextTo.InternalEnergy.Megajoules, 12);
        Assert.Equal(999d, nextFrom.Mass.Kilograms, 12);
        Assert.Equal(801d, nextTo.Mass.Kilograms, 12);
    }

    [Fact]
    public void ValveModel_ComposesWithDeterministicFixedStepRuntime()
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

    [Fact]
    public void ClosedValve_RuntimePreservesBothNodeInventories()
    {
        var runtime = new SimulationRuntime<PlantState, NoCommand, PlantSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            new PlantState(
                CreateNode("from", 5.4d, 1_000d, 100d),
                CreateNode("to", 5.0d, 800d, 120d),
                new ValveState("valve", ValvePosition.Closed)),
            new PlantKernel());

        runtime.Resume();
        runtime.Advance(TimeSpan.FromSeconds(2d));

        var snapshot = runtime.GetSnapshot();

        Assert.Equal(1_000d, snapshot.State.FromMassKilograms, 12);
        Assert.Equal(800d, snapshot.State.ToMassKilograms, 12);
        Assert.Equal(220d, snapshot.State.TotalInternalEnergyMegajoules, 12);
    }

    private static SimulationRuntime<PlantState, NoCommand, PlantSnapshot> CreateRuntime()
    {
        return new SimulationRuntime<PlantState, NoCommand, PlantSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            new PlantState(
                CreateNode("from", 5.4d, 1_000d, 100d),
                CreateNode("to", 5.0d, 800d, 120d),
                new ValveState("valve", ValvePosition.FromPercent(50d))),
            new PlantKernel());
    }

    private static ValveDefinition CreateValve()
    {
        return new ValveDefinition(
            "valve",
            new PipeDefinition(
                "pipe",
                "from",
                "to",
                QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000d)),
            ValveCharacteristic.Linear,
            ValveFailSafeAction.FailClosed);
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

    private sealed record PlantState(
        FluidNodeState FromNode,
        FluidNodeState ToNode,
        ValveState ValveState);

    private sealed record PlantSnapshot(
        double FromMassKilograms,
        double ToMassKilograms,
        double TotalMassKilograms,
        double TotalInternalEnergyMegajoules);

    private sealed class PlantKernel : ISimulationKernel<PlantState, NoCommand, PlantSnapshot>
    {
        private readonly ValveFlowSolver _flowSolver = new();
        private readonly ValveDefinition _valve = CreateValve();
        private readonly FluidNodeIntegrator _integrator = new(new StableThermodynamicModel());

        public PlantState Step(
            PlantState state,
            IReadOnlyList<QueuedSimulationCommand<NoCommand>> commands,
            SimulationStepContext context)
        {
            _ = commands;
            var flow = _flowSolver.Solve(_valve, state.ValveState, state.FromNode, state.ToNode);

            return state with
            {
                FromNode = _integrator.Step(state.FromNode, flow.FromNodeBalance, context.DeltaTime),
                ToNode = _integrator.Step(state.ToNode, flow.ToNodeBalance, context.DeltaTime),
            };
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
