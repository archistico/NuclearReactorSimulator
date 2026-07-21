using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Reactor.Core;

public sealed class AggregatedCoreDefinitionTests
{
    [Fact]
    public void Definition_CanonicalizesZonesWithoutHardcodingGridSize()
    {
        var plant = CreatePlant("a", "b", "c", "d");
        var zones = new[]
        {
            Zone("zone-d", 9, 2, 25, "d"),
            Zone("zone-b", 0, 7, 25, "b"),
            Zone("zone-a", 0, 0, 25, "a"),
            Zone("zone-c", 4, 1, 25, "c"),
        };

        var definition = new AggregatedCoreDefinition("core", plant, zones);

        Assert.Equal(new[] { "zone-a", "zone-b", "zone-c", "zone-d" }, definition.Zones.Select(static zone => zone.Id));
        Assert.Equal(new CoreZoneCoordinate(9, 2), definition.GetZone("zone-d").Coordinate);
    }

    [Fact]
    public void Definition_RejectsDuplicateLogicalCoordinates()
    {
        var plant = CreatePlant("a", "b");
        var zones = new[]
        {
            Zone("zone-a", 0, 0, 50, "a"),
            Zone("zone-b", 0, 0, 50, "b"),
        };

        Assert.Throws<ArgumentException>(() => new AggregatedCoreDefinition("core", plant, zones));
    }

    [Fact]
    public void Definition_RejectsPowerFractionsThatDoNotClose()
    {
        var plant = CreatePlant("a", "b");
        var zones = new[]
        {
            Zone("zone-a", 0, 0, 40, "a"),
            Zone("zone-b", 0, 1, 40, "b"),
        };

        Assert.Throws<ArgumentException>(() => new AggregatedCoreDefinition("core", plant, zones));
    }

    [Fact]
    public void Definition_RejectsUnknownPlantDomainReference()
    {
        var plant = CreatePlant("a");
        var zone = new CoreZoneDefinition(
            "zone-a",
            new CoreZoneCoordinate(0, 0),
            CoreZonePowerFraction.Full,
            "missing-fuel",
            "structure-a",
            "coolant-a");

        Assert.Throws<KeyNotFoundException>(() => new AggregatedCoreDefinition("core", plant, new[] { zone }));
    }

    [Fact]
    public void State_MustContainExactZoneSetAndNormalizedShares()
    {
        var plant = CreatePlant("a", "b");
        var definition = new AggregatedCoreDefinition(
            "core",
            plant,
            new[] { Zone("zone-a", 0, 0, 50, "a"), Zone("zone-b", 0, 1, 50, "b") });

        Assert.Throws<ArgumentException>(() => new AggregatedCoreState(
            definition,
            new[] { new CoreZoneState("zone-a", CoreZonePowerFraction.Full) }));

        Assert.Throws<ArgumentException>(() => new AggregatedCoreState(
            definition,
            new[]
            {
                new CoreZoneState("zone-a", CoreZonePowerFraction.FromPercent(60)),
                new CoreZoneState("zone-b", CoreZonePowerFraction.FromPercent(30)),
            }));
    }

    [Fact]
    public void CreateNominal_UsesConfiguredPowerDistribution()
    {
        var plant = CreatePlant("a", "b");
        var definition = new AggregatedCoreDefinition(
            "core",
            plant,
            new[] { Zone("zone-a", 0, 0, 30, "a"), Zone("zone-b", 0, 1, 70, "b") });

        var state = AggregatedCoreState.CreateNominal(definition);

        Assert.Equal(30d, state.GetZone("zone-a").PowerFraction.Percent, 12);
        Assert.Equal(70d, state.GetZone("zone-b").PowerFraction.Percent, 12);
    }

    private static CoreZoneDefinition Zone(string id, int row, int column, double percent, string suffix)
        => new(
            id,
            new CoreZoneCoordinate(row, column),
            CoreZonePowerFraction.FromPercent(percent),
            $"fuel-{suffix}",
            $"structure-{suffix}",
            $"coolant-{suffix}");

    private static PlantDefinition CreatePlant(params string[] suffixes)
    {
        var fluidNodes = suffixes.Select(suffix => new FluidNodeDefinition($"coolant-{suffix}", Volume.FromCubicMetres(10))).ToArray();
        var thermalBodies = suffixes.SelectMany(suffix => new[]
        {
            new ThermalBodyDefinition($"fuel-{suffix}", HeatCapacity.FromJoulesPerKelvin(1_000_000)),
            new ThermalBodyDefinition($"structure-{suffix}", HeatCapacity.FromJoulesPerKelvin(2_000_000)),
        }).ToArray();

        return new PlantDefinition(
            "plant",
            fluidNodes,
            Array.Empty<PipeDefinition>(),
            Array.Empty<ValveDefinition>(),
            Array.Empty<PumpDefinition>(),
            thermalBodies,
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());
    }
}
