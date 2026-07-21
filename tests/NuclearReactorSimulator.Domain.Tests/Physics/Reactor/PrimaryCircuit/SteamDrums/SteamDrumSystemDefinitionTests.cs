using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Reactor.PrimaryCircuit.SteamDrums;

public sealed class SteamDrumSystemDefinitionTests
{
    [Fact]
    public void Constructor_AcceptsDedicatedReturnCollectorAndCanonicalSteamDrum()
    {
        var fixture = CreateFixture();
        var definition = new SteamDrumSystemDefinition(
            "drums",
            fixture.Circulation,
            new[] { new SteamDrumDefinition("drum-a", "loop-a", "drum-node", "steam-outlet") });

        Assert.Equal("drum-a", definition.Drums.Single().Id);
        Assert.Equal("drum-node", fixture.Circulation.GetLoop("loop-a").ReturnCollectorNodeId);
    }

    [Fact]
    public void Constructor_RejectsInventoryNodeThatIsNotLoopReturnCollector()
    {
        var fixture = CreateFixture();

        var exception = Assert.Throws<ArgumentException>(() => new SteamDrumSystemDefinition(
            "drums",
            fixture.Circulation,
            new[] { new SteamDrumDefinition("drum-a", "loop-a", "outlet-a", "steam-outlet") }));

        Assert.Contains("must be the return collector", exception.Message);
    }

    [Fact]
    public void Constructor_RequiresExactlyOneDrumPerCirculationLoop()
    {
        var fixture = CreateFixture();

        var exception = Assert.Throws<ArgumentException>(() => new SteamDrumSystemDefinition(
            "drums",
            fixture.Circulation,
            Array.Empty<SteamDrumDefinition>()));

        Assert.Contains("at least one drum", exception.Message);
    }

    private static Fixture CreateFixture()
    {
        var plant = BuildPlant();
        var core = new AggregatedCoreDefinition(
            "core",
            plant,
            new[]
            {
                new CoreZoneDefinition(
                    "zone-a",
                    new CoreZoneCoordinate(0, 0),
                    CoreZonePowerFraction.FromPercent(100),
                    "fuel-a",
                    "structure-a",
                    "outlet-a"),
            });
        var groups = new FuelChannelGroupSetDefinition(
            "channels",
            core,
            new[]
            {
                new FuelChannelGroupDefinition(
                    "group-a",
                    "zone-a",
                    100,
                    CoreZonePowerFraction.FromPercent(100),
                    "channel-a",
                    "pressure-a",
                    "outlet-a",
                    "fuel-a",
                    "structure-a",
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
                    "loop-a",
                    "suction-a",
                    "pressure-a",
                    "drum-node",
                    new[] { "pump-a" },
                    new[] { new MainCirculationBranchDefinition("group-a", "return-a") }),
            });
        return new Fixture(circulation);
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
            new[] { Node("suction-a"), Node("pressure-a"), Node("outlet-a"), Node("drum-node"), Node("steam-outlet") },
            new[] { Pipe("channel-a", "pressure-a", "outlet-a"), Pipe("return-a", "outlet-a", "drum-node") },
            Array.Empty<ValveDefinition>(),
            new[]
            {
                new PumpDefinition(
                    "pump-a",
                    Pipe("pump-a-path", "suction-a", "pressure-a"),
                    PressureDifference.FromMegapascals(1.5),
                    QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000),
                    PumpEfficiency.FromPercent(80)),
            },
            new[]
            {
                new ThermalBodyDefinition("fuel-a", HeatCapacity.FromJoulesPerKelvin(10_000_000)),
                new ThermalBodyDefinition("structure-a", HeatCapacity.FromJoulesPerKelvin(20_000_000)),
            },
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());
    }

    private sealed record Fixture(MainCirculationSystemDefinition Circulation);
}
