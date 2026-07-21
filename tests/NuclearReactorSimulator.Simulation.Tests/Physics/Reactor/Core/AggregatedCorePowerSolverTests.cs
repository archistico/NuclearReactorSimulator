using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.Core;

public sealed class AggregatedCorePowerSolverTests
{
    [Fact]
    public void Solve_PartitionsGlobalPowerAndClosesExactly()
    {
        var fixture = CreateFixture();
        var state = new AggregatedCoreState(
            fixture.Core,
            new[]
            {
                new CoreZoneState("zone-a", CoreZonePowerFraction.FromPercent(20)),
                new CoreZoneState("zone-b", CoreZonePowerFraction.FromPercent(30)),
                new CoreZoneState("zone-c", CoreZonePowerFraction.FromPercent(50)),
            });

        var snapshot = new AggregatedCorePowerSolver(fixture.Core).Solve(state, Power.FromMegawatts(1000), fixture.PlantState);

        Assert.Equal(200d, snapshot.GetZone("zone-a").FissionThermalPower.Megawatts, 9);
        Assert.Equal(300d, snapshot.GetZone("zone-b").FissionThermalPower.Megawatts, 9);
        Assert.Equal(500d, snapshot.GetZone("zone-c").FissionThermalPower.Megawatts, 9);
        Assert.Equal(snapshot.TotalFissionThermalPower.Watts, snapshot.Zones.Sum(static zone => zone.FissionThermalPower.Watts));
    }

    [Fact]
    public void Solve_UsesCommittedPlantDomainsForLocalDiagnostics()
    {
        var fixture = CreateFixture();
        var state = AggregatedCoreState.CreateNominal(fixture.Core);

        var snapshot = new AggregatedCorePowerSolver(fixture.Core).Solve(state, Power.FromMegawatts(900), fixture.PlantState);
        var zone = snapshot.GetZone("zone-b");

        Assert.Equal(710d, zone.FuelTemperature.DegreesCelsius, 9);
        Assert.Equal(510d, zone.StructureTemperature.DegreesCelsius, 9);
        Assert.Equal(285d, zone.CoolantTemperature.DegreesCelsius, 9);
        Assert.Equal(6.9d, zone.CoolantPressure.Megapascals, 9);
        Assert.Equal(FluidPhase.SubcooledLiquid, zone.CoolantPhase);
        Assert.NotNull(zone.VoidFraction);
        Assert.Equal(0d, zone.VoidFraction.Value.Fraction, 12);
    }

    [Fact]
    public void Solve_IsIndependentFromCallerZoneOrder()
    {
        var fixture = CreateFixture();
        var stateA = new AggregatedCoreState(
            fixture.Core,
            new[]
            {
                new CoreZoneState("zone-a", CoreZonePowerFraction.FromPercent(10)),
                new CoreZoneState("zone-b", CoreZonePowerFraction.FromPercent(20)),
                new CoreZoneState("zone-c", CoreZonePowerFraction.FromPercent(70)),
            });
        var stateB = new AggregatedCoreState(
            fixture.Core,
            new[]
            {
                new CoreZoneState("zone-c", CoreZonePowerFraction.FromPercent(70)),
                new CoreZoneState("zone-a", CoreZonePowerFraction.FromPercent(10)),
                new CoreZoneState("zone-b", CoreZonePowerFraction.FromPercent(20)),
            });
        var solver = new AggregatedCorePowerSolver(fixture.Core);

        var snapshotA = solver.Solve(stateA, Power.FromMegawatts(1200), fixture.PlantState);
        var snapshotB = solver.Solve(stateB, Power.FromMegawatts(1200), fixture.PlantState);

        Assert.Equal(snapshotA.Zones.ToArray(), snapshotB.Zones.ToArray());
    }

    [Fact]
    public void Solve_DoesNotRequireThreeByThreeGrid()
    {
        var fixture = CreateFixture();

        Assert.Equal(3, fixture.Core.Zones.Count);
        Assert.Equal(new CoreZoneCoordinate(7, 11), fixture.Core.GetZone("zone-c").Coordinate);
    }

    private static Fixture CreateFixture()
    {
        var suffixes = new[] { "a", "b", "c" };
        var fluidDefinitions = suffixes.Select(suffix => new FluidNodeDefinition($"coolant-{suffix}", Volume.FromCubicMetres(10))).ToArray();
        var thermalDefinitions = suffixes.SelectMany(suffix => new[]
        {
            new ThermalBodyDefinition($"fuel-{suffix}", HeatCapacity.FromJoulesPerKelvin(1_000_000)),
            new ThermalBodyDefinition($"structure-{suffix}", HeatCapacity.FromJoulesPerKelvin(2_000_000)),
        }).ToArray();
        var plant = new PlantDefinition(
            "plant",
            fluidDefinitions,
            Array.Empty<PipeDefinition>(),
            Array.Empty<ValveDefinition>(),
            Array.Empty<PumpDefinition>(),
            thermalDefinitions,
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());

        var fluidStates = suffixes.Select(suffix =>
        {
            var index = Array.IndexOf(suffixes, suffix);
            return new FluidNodeState(
                plant.GetFluidNode($"coolant-{suffix}"),
                new FluidNodeInventory(Mass.FromKilograms(8_000), Energy.FromMegajoules(5_000)),
                new FluidThermodynamicState(
                    Pressure.FromMegapascals(6.8 + (index * 0.1)),
                    Temperature.FromDegreesCelsius(280 + (index * 5)),
                    FluidPhase.SubcooledLiquid,
                    null));
        }).ToArray();

        var thermalStates = suffixes.SelectMany((suffix, index) => new[]
        {
            ThermalBodyState.FromTemperature(plant.GetThermalBody($"fuel-{suffix}"), Temperature.FromDegreesCelsius(700 + (index * 10))),
            ThermalBodyState.FromTemperature(plant.GetThermalBody($"structure-{suffix}"), Temperature.FromDegreesCelsius(500 + (index * 10))),
        }).ToArray();

        var plantState = new PlantState(
            plant,
            fluidStates,
            Array.Empty<ValveState>(),
            Array.Empty<PumpState>(),
            thermalStates,
            Array.Empty<HeatSourceState>());

        var core = new AggregatedCoreDefinition(
            "core",
            plant,
            new[]
            {
                new CoreZoneDefinition("zone-c", new CoreZoneCoordinate(7, 11), CoreZonePowerFraction.FromPercent(50), "fuel-c", "structure-c", "coolant-c"),
                new CoreZoneDefinition("zone-a", new CoreZoneCoordinate(0, 0), CoreZonePowerFraction.FromPercent(20), "fuel-a", "structure-a", "coolant-a"),
                new CoreZoneDefinition("zone-b", new CoreZoneCoordinate(2, 5), CoreZonePowerFraction.FromPercent(30), "fuel-b", "structure-b", "coolant-b"),
            });

        return new Fixture(core, plantState);
    }

    private sealed record Fixture(AggregatedCoreDefinition Core, PlantState PlantState);
}
