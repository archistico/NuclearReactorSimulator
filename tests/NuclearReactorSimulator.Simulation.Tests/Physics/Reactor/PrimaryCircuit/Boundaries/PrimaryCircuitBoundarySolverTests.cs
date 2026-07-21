using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.PrimaryCircuit.Boundaries;

public sealed class PrimaryCircuitBoundarySolverTests
{
    [Fact]
    public void Solve_ProducesSignedExternalMassAndEnergyAccounting()
    {
        var fixture = CreateFixture();
        var result = fixture.BoundarySolver.Solve(fixture.State, CreateInputs(fixture.Boundaries));
        var feedwater = result.Snapshot.GetFeedwaterBoundary("feed-a");
        var steamExport = result.Snapshot.GetSteamExportBoundary("steam-a");

        Assert.Equal(3d, feedwater.MassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(3d, feedwater.EnergyInputRate.Megawatts, 12);
        Assert.Equal(2d, steamExport.MassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(
            fixture.State.GetFluidNode("steam-outlet").SpecificInternalEnergy,
            steamExport.ExportedSpecificInternalEnergy);
        Assert.Equal(1d, result.SourceTerms.ExternalMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(
            feedwater.EnergyInputRate.Watts - steamExport.EnergyExportRate.Watts,
            result.SourceTerms.ExternalPower.Watts,
            9);
        Assert.Equal(result.SourceTerms.ExternalMassFlowRate, result.Snapshot.NetExternalMassFlowRate);
        Assert.Equal(result.SourceTerms.ExternalPower, result.Snapshot.NetExternalPower);
    }

    [Fact]
    public void NetworkOrchestrator_WithBoundaryTerms_ClosesAgainstDeclaredExternalMassAndPower()
    {
        var fixture = CreateFixture();
        var boundary = fixture.BoundarySolver.Solve(fixture.State, CreateInputs(fixture.Boundaries));

        var network = new PlantNetworkOrchestrator(new PreservingThermodynamicModel())
            .Step(fixture.State, TimeSpan.FromMilliseconds(20), boundary.SourceTerms);

        Assert.Equal(boundary.SourceTerms.ExternalMassFlowRate, network.Audit.ExpectedExternalMassFlowRate);
        Assert.Equal(boundary.SourceTerms.ExternalMassFlowRate, network.Audit.SupplementalExternalMassFlowRate);
        Assert.InRange(Math.Abs(network.Audit.BalanceMassRateResidualKilogramsPerSecond), 0d, 1e-9d);
        Assert.InRange(Math.Abs(network.Audit.MassClosureResidualKilograms), 0d, 1e-6d);
        Assert.InRange(Math.Abs(network.Audit.BalancePowerResidualWatts), 0d, 1e-3d);
        Assert.InRange(Math.Abs(network.Audit.EnergyClosureResidualJoules), 0d, 10d);
        Assert.True(network.Audit.IsBalanceMassRateClosedWithin(1e-9d));
    }

    [Fact]
    public void SourceTerms_CombineInternalDrumSeparationWithExternalBoundariesWithoutHidingBoundaryExchange()
    {
        var fixture = CreateFixture();
        var separation = fixture.DrumSolver.Solve(fixture.State);
        var boundary = fixture.BoundarySolver.Solve(fixture.State, CreateInputs(fixture.Boundaries));

        var combined = PlantNetworkSourceTerms.Combine(separation.SourceTerms, boundary.SourceTerms);
        var network = new PlantNetworkOrchestrator(new PreservingThermodynamicModel())
            .Step(fixture.State, TimeSpan.FromMilliseconds(20), combined);

        Assert.Equal(boundary.SourceTerms.ExternalMassFlowRate, combined.ExternalMassFlowRate);
        Assert.Equal(boundary.SourceTerms.ExternalPower, combined.ExternalPower);
        Assert.InRange(Math.Abs(network.Audit.BalanceMassRateResidualKilogramsPerSecond), 0d, 1e-9d);
        Assert.InRange(Math.Abs(network.Audit.BalancePowerResidualWatts), 0d, 1e-3d);
        Assert.InRange(Math.Abs(network.Audit.EnergyClosureResidualJoules), 0d, 10d);
    }

    [Fact]
    public void Inputs_RequireExactlyOneInputForEveryDefinedBoundary()
    {
        var fixture = CreateFixture();

        var exception = Assert.Throws<ArgumentException>(() => new PrimaryCircuitBoundaryInputs(
            fixture.Boundaries,
            Array.Empty<FeedwaterBoundaryInput>(),
            new[] { new SteamExportBoundaryInput("steam-a", MassFlowRate.Zero) }));

        Assert.Contains("exactly one input", exception.Message);
    }

    private static PrimaryCircuitBoundaryInputs CreateInputs(PrimaryCircuitBoundarySystemDefinition definition)
        => new(
            definition,
            new[]
            {
                new FeedwaterBoundaryInput(
                    "feed-a",
                    MassFlowRate.FromKilogramsPerSecond(3d),
                    SpecificEnergy.FromKilojoulesPerKilogram(1_000d)),
            },
            new[]
            {
                new SteamExportBoundaryInput("steam-a", MassFlowRate.FromKilogramsPerSecond(2d)),
            });

    private static Fixture CreateFixture()
    {
        var plant = BuildPlant();

        FluidNodeState Fluid(string id, double pressureMpa, double temperatureCelsius, double massKg, double energyMj)
            => new(
                plant.GetFluidNode(id),
                new FluidNodeInventory(Mass.FromKilograms(massKg), Energy.FromMegajoules(energyMj)),
                new FluidThermodynamicState(
                    Pressure.FromMegapascals(pressureMpa),
                    Temperature.FromDegreesCelsius(temperatureCelsius),
                    FluidPhase.SubcooledLiquid,
                    null));

        var state = new PlantState(
            plant,
            new[]
            {
                Fluid("suction", 6.0d, 270d, 8_000d, 5_000d),
                Fluid("pressure", 7.4d, 272d, 8_000d, 5_000d),
                Fluid("outlet", 7.0d, 285d, 8_000d, 5_000d),
                Fluid("drum-node", 6.2d, 280d, 8_000d, 5_000d),
                Fluid("steam-outlet", 6.0d, 280d, 500d, 5_000d),
            },
            Array.Empty<ValveState>(),
            new[] { new PumpState("mcp", PumpSpeed.Rated) },
            new[]
            {
                ThermalBodyState.FromTemperature(plant.GetThermalBody("fuel"), Temperature.FromDegreesCelsius(700d)),
                ThermalBodyState.FromTemperature(plant.GetThermalBody("structure"), Temperature.FromDegreesCelsius(500d)),
            },
            Array.Empty<HeatSourceState>());

        var core = new AggregatedCoreDefinition(
            "core",
            plant,
            new[]
            {
                new CoreZoneDefinition(
                    "zone",
                    new CoreZoneCoordinate(0, 0),
                    CoreZonePowerFraction.FromPercent(100),
                    "fuel",
                    "structure",
                    "outlet"),
            });
        var groups = new FuelChannelGroupSetDefinition(
            "channels",
            core,
            new[]
            {
                new FuelChannelGroupDefinition(
                    "group",
                    "zone",
                    100,
                    CoreZonePowerFraction.FromPercent(100),
                    "channel",
                    "pressure",
                    "outlet",
                    "fuel",
                    "structure",
                    HeatDepositionFraction.FromPercent(70),
                    HeatDepositionFraction.FromPercent(10),
                    HeatDepositionFraction.FromPercent(20)),
            });
        var circulation = new MainCirculationSystemDefinition(
            "mcs",
            groups,
            new[]
            {
                new MainCirculationLoopDefinition(
                    "loop",
                    "suction",
                    "pressure",
                    "drum-node",
                    new[] { "mcp" },
                    new[] { new MainCirculationBranchDefinition("group", "return") }),
            });
        var drums = new SteamDrumSystemDefinition(
            "drums",
            circulation,
            new[] { new SteamDrumDefinition("drum-a", "loop", "drum-node", "steam-outlet") });
        var boundaries = new PrimaryCircuitBoundarySystemDefinition(
            "boundaries",
            drums,
            new[] { new FeedwaterBoundaryDefinition("feed-a", "drum-a", "drum-node") },
            new[] { new SteamExportBoundaryDefinition("steam-a", "drum-a", "steam-outlet") });

        return new Fixture(
            state,
            boundaries,
            new PrimaryCircuitBoundarySolver(boundaries),
            new SteamDrumSeparationSolver(drums));
    }

    private static PlantDefinition BuildPlant()
    {
        FluidNodeDefinition Node(string id, double volume = 10d) => new(id, Volume.FromCubicMetres(volume));
        PipeDefinition Pipe(string id, string from, string to, double resistance) => new(
            id,
            from,
            to,
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(resistance));

        return new PlantDefinition(
            "plant",
            new[] { Node("suction"), Node("pressure"), Node("outlet"), Node("drum-node"), Node("steam-outlet") },
            new[]
            {
                Pipe("channel", "pressure", "outlet", 100_000d),
                Pipe("return", "outlet", "drum-node", 150_000d),
            },
            Array.Empty<ValveDefinition>(),
            new[]
            {
                new PumpDefinition(
                    "mcp",
                    Pipe("mcp-path", "suction", "pressure", 80_000d),
                    PressureDifference.FromMegapascals(1.8d),
                    QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(40_000d),
                    PumpEfficiency.FromPercent(82d)),
            },
            new[]
            {
                new ThermalBodyDefinition("fuel", HeatCapacity.FromJoulesPerKelvin(10_000_000d)),
                new ThermalBodyDefinition("structure", HeatCapacity.FromJoulesPerKelvin(20_000_000d)),
            },
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());
    }

    private sealed record Fixture(
        PlantState State,
        PrimaryCircuitBoundarySystemDefinition Boundaries,
        PrimaryCircuitBoundarySolver BoundarySolver,
        SteamDrumSeparationSolver DrumSolver);

    private sealed class PreservingThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
            => previousState;
    }
}
