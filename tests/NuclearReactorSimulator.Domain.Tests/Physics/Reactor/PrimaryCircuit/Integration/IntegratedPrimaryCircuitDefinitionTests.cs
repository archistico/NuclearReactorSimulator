using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Reactor.PrimaryCircuit.Integration;

public sealed class IntegratedPrimaryCircuitDefinitionTests
{
    [Fact]
    public void Definition_ExposesOneCanonicalLineageAcrossAllM3PrimaryCircuitLayers()
    {
        var fixture = CreateDefinition();

        Assert.Equal("primary", fixture.Integrated.Id);
        Assert.Same(fixture.Boundaries, fixture.Integrated.BoundarySystem);
        Assert.Same(fixture.Drums, fixture.Integrated.SteamDrumSystem);
        Assert.Same(fixture.Circulation, fixture.Integrated.MainCirculationSystem);
        Assert.Same(fixture.Groups, fixture.Integrated.ChannelGroups);
        Assert.Same(fixture.Core, fixture.Integrated.CoreDefinition);
        Assert.Same(fixture.Plant, fixture.Integrated.PlantDefinition);
    }

    [Fact]
    public void Definition_RejectsEmptyIdentity()
    {
        var fixture = CreateDefinition();

        Assert.Throws<ArgumentException>(() => new IntegratedPrimaryCircuitDefinition(" ", fixture.Boundaries));
    }

    private static Fixture CreateDefinition()
    {
        FluidNodeDefinition Node(string id) => new(id, Volume.FromCubicMetres(10d));
        PipeDefinition Pipe(string id, string from, string to) => new(
            id,
            from,
            to,
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000d));

        var plant = new PlantDefinition(
            "plant",
            new[] { Node("suction"), Node("pressure"), Node("outlet"), Node("drum"), Node("steam") },
            new[] { Pipe("channel", "pressure", "outlet"), Pipe("return", "outlet", "drum") },
            Array.Empty<ValveDefinition>(),
            new[]
            {
                new PumpDefinition(
                    "pump",
                    Pipe("pump-path", "suction", "pressure"),
                    PressureDifference.FromMegapascals(1d),
                    QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000d),
                    PumpEfficiency.FromPercent(80d)),
            },
            new[]
            {
                new ThermalBodyDefinition("fuel", HeatCapacity.FromJoulesPerKelvin(1_000_000d)),
                new ThermalBodyDefinition("structure", HeatCapacity.FromJoulesPerKelvin(1_000_000d)),
            },
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());
        var core = AggregatedCoreDefinition.CreateSingleZone(
            "core",
            plant,
            "zone",
            "fuel",
            "structure",
            "outlet");
        var groups = new FuelChannelGroupSetDefinition(
            "groups",
            core,
            new[]
            {
                new FuelChannelGroupDefinition(
                    "group",
                    "zone",
                    100,
                    CoreZonePowerFraction.Full,
                    "channel",
                    "pressure",
                    "outlet",
                    "fuel",
                    "structure",
                    HeatDepositionFraction.FromPercent(70d),
                    HeatDepositionFraction.FromPercent(10d),
                    HeatDepositionFraction.FromPercent(20d)),
            });
        var circulation = new MainCirculationSystemDefinition(
            "circulation",
            groups,
            new[]
            {
                new MainCirculationLoopDefinition(
                    "loop",
                    "suction",
                    "pressure",
                    "drum",
                    new[] { "pump" },
                    new[] { new MainCirculationBranchDefinition("group", "return") }),
            });
        var drums = new SteamDrumSystemDefinition(
            "drums",
            circulation,
            new[] { new SteamDrumDefinition("drum-a", "loop", "drum", "steam") });
        var boundaries = new PrimaryCircuitBoundarySystemDefinition(
            "boundaries",
            drums,
            new[] { new FeedwaterBoundaryDefinition("feed", "drum-a", "drum") },
            new[] { new SteamExportBoundaryDefinition("export", "drum-a", "steam") });
        var integrated = new IntegratedPrimaryCircuitDefinition("primary", boundaries);

        return new Fixture(plant, core, groups, circulation, drums, boundaries, integrated);
    }

    private sealed record Fixture(
        PlantDefinition Plant,
        AggregatedCoreDefinition Core,
        FuelChannelGroupSetDefinition Groups,
        MainCirculationSystemDefinition Circulation,
        SteamDrumSystemDefinition Drums,
        PrimaryCircuitBoundarySystemDefinition Boundaries,
        IntegratedPrimaryCircuitDefinition Integrated);
}
