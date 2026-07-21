using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Thermal;

public sealed class HeatTransferIntegrationTests
{
    [Fact]
    public void WallToFluidTransfer_ConservesCombinedStoredEnergy()
    {
        var wall = CreateWall(300d);
        var fluid = CreateFluid(250d);
        var transfer = new HeatTransferSolver().Solve(
            CreateLink(),
            wall.Temperature,
            fluid.Temperature);
        var initialTotal = wall.StoredThermalEnergy.Joules + fluid.InternalEnergy.Joules;

        var nextWall = new ThermalBodyIntegrator().Step(
            wall,
            transfer.FromDomainBalance,
            TimeSpan.FromSeconds(1d));
        var nextFluid = new FluidNodeIntegrator(new StableThermodynamicModel()).Step(
            fluid,
            new FluidNodeBalance(MassFlowRate.Zero, transfer.ToDomainBalance.NetHeatRate),
            TimeSpan.FromSeconds(1d));

        Assert.Equal(
            initialTotal,
            nextWall.StoredThermalEnergy.Joules + nextFluid.InternalEnergy.Joules,
            3);
        Assert.True(nextWall.Temperature < wall.Temperature);
        Assert.True(nextFluid.InternalEnergy > fluid.InternalEnergy);
    }

    [Fact]
    public void ExternalHeatSource_AddsExactlyIntegratedEnergy()
    {
        var wall = CreateWall(300d);
        var source = new HeatSourceDefinition("heater", wall.Id, Power.FromMegawatts(2d));
        var balance = new HeatSourceSolver().Solve(source, new HeatSourceState(source.Id));

        var next = new ThermalBodyIntegrator().Step(wall, balance, TimeSpan.FromSeconds(4d));

        Assert.Equal(
            wall.StoredThermalEnergy.Megajoules + 8d,
            next.StoredThermalEnergy.Megajoules,
            9);
    }

    [Fact]
    public void ThermalModel_ComposesWithDeterministicFixedStepRuntime()
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
        Assert.Equal(leftSnapshot.State.InitialTotalEnergyJoules, leftSnapshot.State.TotalEnergyJoules, 3);
        Assert.True(leftSnapshot.State.WallTemperatureCelsius < 300d);
        Assert.True(leftSnapshot.State.FluidInternalEnergyMegajoules > 100d);
    }

    private static SimulationRuntime<PlantState, NoCommand, PlantSnapshot> CreateRuntime()
    {
        var wall = CreateWall(300d);
        var fluid = CreateFluid(250d);
        var initialTotal = wall.StoredThermalEnergy.Joules + fluid.InternalEnergy.Joules;

        return new SimulationRuntime<PlantState, NoCommand, PlantSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            new PlantState(wall, fluid, initialTotal),
            new PlantKernel());
    }

    private static HeatTransferDefinition CreateLink()
    {
        return new HeatTransferDefinition(
            "wall-fluid",
            "wall",
            "fluid",
            ThermalConductance.FromKilowattsPerKelvin(10d));
    }

    private static ThermalBodyState CreateWall(double celsius)
    {
        return ThermalBodyState.FromTemperature(
            new ThermalBodyDefinition(
                "wall",
                HeatCapacity.FromMegajoulesPerKelvin(10d)),
            Temperature.FromDegreesCelsius(celsius));
    }

    private static FluidNodeState CreateFluid(double celsius)
    {
        return new FluidNodeState(
            new FluidNodeDefinition("fluid", Volume.FromCubicMetres(1d)),
            new FluidNodeInventory(
                Mass.FromKilograms(1_000d),
                Energy.FromMegajoules(100d)),
            new FluidThermodynamicState(
                Pressure.FromMegapascals(5d),
                Temperature.FromDegreesCelsius(celsius)));
    }

    private sealed record NoCommand();

    private sealed record PlantState(
        ThermalBodyState Wall,
        FluidNodeState Fluid,
        double InitialTotalEnergyJoules);

    private sealed record PlantSnapshot(
        double WallTemperatureCelsius,
        double FluidInternalEnergyMegajoules,
        double TotalEnergyJoules,
        double InitialTotalEnergyJoules);

    private sealed class PlantKernel : ISimulationKernel<PlantState, NoCommand, PlantSnapshot>
    {
        private readonly HeatTransferDefinition _link = CreateLink();
        private readonly HeatTransferSolver _heatTransferSolver = new();
        private readonly ThermalBodyIntegrator _thermalIntegrator = new();
        private readonly FluidNodeIntegrator _fluidIntegrator = new(new StableThermodynamicModel());

        public PlantState Step(
            PlantState state,
            IReadOnlyList<QueuedSimulationCommand<NoCommand>> commands,
            SimulationStepContext context)
        {
            _ = commands;
            var transfer = _heatTransferSolver.Solve(
                _link,
                state.Wall.Temperature,
                state.Fluid.Temperature);

            return state with
            {
                Wall = _thermalIntegrator.Step(
                    state.Wall,
                    transfer.FromDomainBalance,
                    context.DeltaTime),
                Fluid = _fluidIntegrator.Step(
                    state.Fluid,
                    new FluidNodeBalance(MassFlowRate.Zero, transfer.ToDomainBalance.NetHeatRate),
                    context.DeltaTime),
            };
        }

        public PlantSnapshot CreateSnapshot(PlantState state)
        {
            return new PlantSnapshot(
                state.Wall.Temperature.DegreesCelsius,
                state.Fluid.InternalEnergy.Megajoules,
                state.Wall.StoredThermalEnergy.Joules + state.Fluid.InternalEnergy.Joules,
                state.InitialTotalEnergyJoules);
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
