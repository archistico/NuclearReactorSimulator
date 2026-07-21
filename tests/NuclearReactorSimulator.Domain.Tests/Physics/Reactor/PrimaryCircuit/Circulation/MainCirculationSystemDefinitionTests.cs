using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Reactor.PrimaryCircuit.Circulation;

public sealed class MainCirculationSystemDefinitionTests
{
    [Fact]
    public void Constructor_CanonicalizesLoopsPumpsAndBranches()
    {
        var fixture = CreateFixture();
        var definition = new MainCirculationSystemDefinition(
            "mcs",
            fixture.ChannelGroups,
            new[]
            {
                new MainCirculationLoopDefinition(
                    "loop-b",
                    "suction-b",
                    "pressure-b",
                    new[] { "pump-b2", "pump-b1" },
                    new[] { new MainCirculationBranchDefinition("group-b", "return-b") }),
                new MainCirculationLoopDefinition(
                    "loop-a",
                    "suction-a",
                    "pressure-a",
                    new[] { "pump-a2", "pump-a1" },
                    new[] { new MainCirculationBranchDefinition("group-a", "return-a") }),
            });

        Assert.Equal(new[] { "loop-a", "loop-b" }, definition.Loops.Select(static loop => loop.Id));
        Assert.Equal(new[] { "pump-a1", "pump-a2" }, definition.GetLoop("loop-a").PumpIds);
        Assert.Equal("group-a", definition.GetLoop("loop-a").Branches.Single().FuelChannelGroupId);
    }

    [Fact]
    public void Constructor_RejectsPumpThatDoesNotConnectLoopHeaders()
    {
        var fixture = CreateFixture();

        var exception = Assert.Throws<ArgumentException>(() => new MainCirculationSystemDefinition(
            "mcs",
            fixture.ChannelGroups,
            new[]
            {
                new MainCirculationLoopDefinition(
                    "loop-a",
                    "suction-b",
                    "pressure-a",
                    new[] { "pump-a1" },
                    new[] { new MainCirculationBranchDefinition("group-a", "return-a") }),
                ValidLoopB(),
            }));

        Assert.Contains("must run from suction header", exception.Message);
    }

    [Fact]
    public void Constructor_RejectsReturnPipeThatDoesNotCloseBranchToSuctionHeader()
    {
        var fixture = CreateFixture();

        var exception = Assert.Throws<ArgumentException>(() => new MainCirculationSystemDefinition(
            "mcs",
            fixture.ChannelGroups,
            new[]
            {
                new MainCirculationLoopDefinition(
                    "loop-a",
                    "suction-a",
                    "pressure-a",
                    new[] { "pump-a1" },
                    new[] { new MainCirculationBranchDefinition("group-a", "channel-b") }),
                ValidLoopB(),
            }));

        Assert.Contains("must run from outlet", exception.Message);
    }

    [Fact]
    public void Constructor_RequiresEveryFuelChannelGroupExactlyOnce()
    {
        var fixture = CreateFixture();

        var exception = Assert.Throws<ArgumentException>(() => new MainCirculationSystemDefinition(
            "mcs",
            fixture.ChannelGroups,
            new[]
            {
                new MainCirculationLoopDefinition(
                    "loop-a",
                    "suction-a",
                    "pressure-a",
                    new[] { "pump-a1" },
                    new[] { new MainCirculationBranchDefinition("group-a", "return-a") }),
            }));

        Assert.Contains("Every fuel-channel group", exception.Message);
        Assert.Contains("group-b", exception.Message);
    }

    private static MainCirculationLoopDefinition ValidLoopB()
        => new(
            "loop-b",
            "suction-b",
            "pressure-b",
            new[] { "pump-b1" },
            new[] { new MainCirculationBranchDefinition("group-b", "return-b") });

    private static Fixture CreateFixture()
    {
        var plant = BuildPlant();
        var core = new AggregatedCoreDefinition(
            "core",
            plant,
            new[]
            {
                new CoreZoneDefinition("zone-a", new CoreZoneCoordinate(0, 0), CoreZonePowerFraction.FromPercent(50), "fuel-a", "structure-a", "outlet-a"),
                new CoreZoneDefinition("zone-b", new CoreZoneCoordinate(0, 1), CoreZonePowerFraction.FromPercent(50), "fuel-b", "structure-b", "outlet-b"),
            });
        var groups = new FuelChannelGroupSetDefinition(
            "channels",
            core,
            new[]
            {
                Group("group-a", "zone-a", "channel-a", "pressure-a", "outlet-a", "fuel-a", "structure-a"),
                Group("group-b", "zone-b", "channel-b", "pressure-b", "outlet-b", "fuel-b", "structure-b"),
            });
        return new Fixture(groups);
    }

    private static FuelChannelGroupDefinition Group(
        string id,
        string zoneId,
        string pipeId,
        string inlet,
        string outlet,
        string fuel,
        string structure)
        => new(
            id,
            zoneId,
            100,
            CoreZonePowerFraction.FromPercent(100),
            pipeId,
            inlet,
            outlet,
            fuel,
            structure,
            HeatDepositionFraction.FromPercent(70),
            HeatDepositionFraction.FromPercent(10),
            HeatDepositionFraction.FromPercent(20));

    private static PlantDefinition BuildPlant()
    {
        FluidNodeDefinition Node(string id) => new(id, Volume.FromCubicMetres(10));
        PipeDefinition Pipe(string id, string from, string to) => new(id, from, to, QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000));
        PumpDefinition Pump(string id, string pathId, string from, string to) => new(
            id,
            Pipe(pathId, from, to),
            PressureDifference.FromMegapascals(1.5),
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000),
            PumpEfficiency.FromPercent(80));

        return new PlantDefinition(
            "plant",
            new[]
            {
                Node("suction-a"), Node("pressure-a"), Node("outlet-a"),
                Node("suction-b"), Node("pressure-b"), Node("outlet-b"),
            },
            new[]
            {
                Pipe("channel-a", "pressure-a", "outlet-a"),
                Pipe("return-a", "outlet-a", "suction-a"),
                Pipe("channel-b", "pressure-b", "outlet-b"),
                Pipe("return-b", "outlet-b", "suction-b"),
            },
            Array.Empty<ValveDefinition>(),
            new[]
            {
                Pump("pump-a1", "pump-a1-path", "suction-a", "pressure-a"),
                Pump("pump-a2", "pump-a2-path", "suction-a", "pressure-a"),
                Pump("pump-b1", "pump-b1-path", "suction-b", "pressure-b"),
                Pump("pump-b2", "pump-b2-path", "suction-b", "pressure-b"),
            },
            new[]
            {
                new ThermalBodyDefinition("fuel-a", HeatCapacity.FromJoulesPerKelvin(10_000_000)),
                new ThermalBodyDefinition("structure-a", HeatCapacity.FromJoulesPerKelvin(20_000_000)),
                new ThermalBodyDefinition("fuel-b", HeatCapacity.FromJoulesPerKelvin(10_000_000)),
                new ThermalBodyDefinition("structure-b", HeatCapacity.FromJoulesPerKelvin(20_000_000)),
            },
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());
    }

    private sealed record Fixture(FuelChannelGroupSetDefinition ChannelGroups);
}
