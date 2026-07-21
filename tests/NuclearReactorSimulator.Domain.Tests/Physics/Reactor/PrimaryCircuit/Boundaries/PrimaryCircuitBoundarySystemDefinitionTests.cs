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
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Reactor.PrimaryCircuit.Boundaries;

public sealed class PrimaryCircuitBoundarySystemDefinitionTests
{
    [Fact]
    public void Constructor_AcceptsExactlyOneFeedwaterAndSteamExportBoundaryPerDrum()
    {
        var drums = CreateSteamDrumSystem();

        var definition = new PrimaryCircuitBoundarySystemDefinition(
            "boundaries",
            drums,
            new[] { new FeedwaterBoundaryDefinition("feed-a", "drum-a", "drum-node") },
            new[] { new SteamExportBoundaryDefinition("steam-a", "drum-a", "steam-outlet") });

        Assert.Equal("drum-node", definition.GetFeedwaterBoundary("feed-a").TargetNodeId);
        Assert.Equal("steam-outlet", definition.GetSteamExportBoundary("steam-a").SourceNodeId);
    }

    [Fact]
    public void Constructor_RejectsFeedwaterTargetThatIsNotDrumInventory()
    {
        var drums = CreateSteamDrumSystem();

        var exception = Assert.Throws<ArgumentException>(() => new PrimaryCircuitBoundarySystemDefinition(
            "boundaries",
            drums,
            new[] { new FeedwaterBoundaryDefinition("feed-a", "drum-a", "suction") },
            new[] { new SteamExportBoundaryDefinition("steam-a", "drum-a", "steam-outlet") }));

        Assert.Contains("must target", exception.Message);
    }

    [Fact]
    public void Constructor_RejectsSteamExportSourceThatIsNotCanonicalDrumOutlet()
    {
        var drums = CreateSteamDrumSystem();

        var exception = Assert.Throws<ArgumentException>(() => new PrimaryCircuitBoundarySystemDefinition(
            "boundaries",
            drums,
            new[] { new FeedwaterBoundaryDefinition("feed-a", "drum-a", "drum-node") },
            new[] { new SteamExportBoundaryDefinition("steam-a", "drum-a", "outlet") }));

        Assert.Contains("must source", exception.Message);
    }

    private static SteamDrumSystemDefinition CreateSteamDrumSystem()
    {
        var plant = BuildPlant();
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

        return new SteamDrumSystemDefinition(
            "drums",
            circulation,
            new[] { new SteamDrumDefinition("drum-a", "loop", "drum-node", "steam-outlet") });
    }

    private static PlantDefinition BuildPlant()
    {
        FluidNodeDefinition Node(string id) => new(id, Volume.FromCubicMetres(10));
        PipeDefinition Pipe(string id, string from, string to) => new(
            id,
            from,
            to,
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000));

        return new PlantDefinition(
            "plant",
            new[] { Node("suction"), Node("pressure"), Node("outlet"), Node("drum-node"), Node("steam-outlet") },
            new[] { Pipe("channel", "pressure", "outlet"), Pipe("return", "outlet", "drum-node") },
            Array.Empty<ValveDefinition>(),
            new[]
            {
                new PumpDefinition(
                    "mcp",
                    Pipe("mcp-path", "suction", "pressure"),
                    PressureDifference.FromMegapascals(1.5),
                    QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000),
                    PumpEfficiency.FromPercent(80)),
            },
            new[]
            {
                new ThermalBodyDefinition("fuel", HeatCapacity.FromJoulesPerKelvin(10_000_000)),
                new ThermalBodyDefinition("structure", HeatCapacity.FromJoulesPerKelvin(20_000_000)),
            },
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());
    }
}
