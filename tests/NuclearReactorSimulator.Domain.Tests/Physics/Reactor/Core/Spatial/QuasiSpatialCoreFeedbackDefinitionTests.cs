using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Spatial;
using NuclearReactorSimulator.Domain.Physics.Reactor.Feedback;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Reactor.Core.Spatial;

public sealed class QuasiSpatialCoreFeedbackDefinitionTests
{
    [Fact]
    public void Definition_CanonicalizesExplicitCouplingsWithoutAssumingGridAdjacency()
    {
        var core = CreateCore();
        var definition = CreateDefinition(
            core,
            new[]
            {
                new CoreZoneCouplingDefinition("zone-c", "zone-a", CoreZoneCouplingFraction.FromPercent(10d)),
                new CoreZoneCouplingDefinition("zone-b", "zone-c", CoreZoneCouplingFraction.FromPercent(20d)),
            });

        Assert.Equal(2, definition.Couplings.Count);
        Assert.Equal("zone-c", definition.Couplings[0].FirstZoneId);
        Assert.Equal("zone-a", definition.Couplings[0].SecondZoneId);
        Assert.Equal(new CoreZoneCoordinate(7, 11), definition.CoreDefinition.GetZone("zone-c").Coordinate);
    }

    [Fact]
    public void Definition_RejectsDuplicateUndirectedCoupling()
    {
        var core = CreateCore();

        Assert.Throws<ArgumentException>(() => CreateDefinition(
            core,
            new[]
            {
                new CoreZoneCouplingDefinition("zone-a", "zone-b", CoreZoneCouplingFraction.FromPercent(10d)),
                new CoreZoneCouplingDefinition("zone-b", "zone-a", CoreZoneCouplingFraction.FromPercent(20d)),
            }));
    }

    [Fact]
    public void Definition_RejectsIncidentCouplingAboveUnity()
    {
        var core = CreateCore();

        Assert.Throws<ArgumentException>(() => CreateDefinition(
            core,
            new[]
            {
                new CoreZoneCouplingDefinition("zone-a", "zone-b", CoreZoneCouplingFraction.FromPercent(60d)),
                new CoreZoneCouplingDefinition("zone-a", "zone-c", CoreZoneCouplingFraction.FromPercent(50d)),
            }));
    }

    [Fact]
    public void Definition_SupportsHigherResolutionAggregationsWithoutFixedZoneCount()
    {
        var plant = CreatePlant("a", "b", "c", "d", "e");
        var core = new AggregatedCoreDefinition(
            "core-5",
            plant,
            new[]
            {
                Zone("zone-a", 0, 0, 20d, "a"),
                Zone("zone-b", 0, 3, 20d, "b"),
                Zone("zone-c", 2, 1, 20d, "c"),
                Zone("zone-d", 5, 4, 20d, "d"),
                Zone("zone-e", 8, 9, 20d, "e"),
            });

        var definition = CreateDefinition(core, Array.Empty<CoreZoneCouplingDefinition>());

        Assert.Equal(5, definition.CoreDefinition.Zones.Count);
    }

    private static QuasiSpatialCoreFeedbackDefinition CreateDefinition(
        AggregatedCoreDefinition core,
        IEnumerable<CoreZoneCouplingDefinition> couplings)
        => new(
            "quasi-spatial",
            core,
            new TemperatureReactivityFeedbackDefinition(
                "fuel-temperature",
                ReactivityContributionKind.FuelTemperature,
                Temperature.FromDegreesCelsius(700d),
                TemperatureReactivityCoefficient.FromPcmPerKelvin(-1d)),
            new TemperatureReactivityFeedbackDefinition(
                "coolant-temperature",
                ReactivityContributionKind.CoolantTemperature,
                Temperature.FromDegreesCelsius(280d),
                TemperatureReactivityCoefficient.FromPcmPerKelvin(-0.2d)),
            new VoidReactivityFeedbackDefinition(
                "void",
                VoidFraction.NoVoid,
                VoidReactivityCoefficient.FromPcmPerPercentVoid(2d)),
            CorePowerShapeSensitivity.FromPerPcm(0.002d),
            TimeSpan.FromSeconds(2d),
            couplings);

    private static AggregatedCoreDefinition CreateCore()
    {
        var plant = CreatePlant("a", "b", "c");
        return new AggregatedCoreDefinition(
            "core",
            plant,
            new[]
            {
                Zone("zone-c", 7, 11, 50d, "c"),
                Zone("zone-a", 0, 0, 20d, "a"),
                Zone("zone-b", 2, 5, 30d, "b"),
            });
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
        => new(
            "plant",
            suffixes.Select(suffix => new FluidNodeDefinition($"coolant-{suffix}", Volume.FromCubicMetres(10d))).ToArray(),
            Array.Empty<PipeDefinition>(),
            Array.Empty<ValveDefinition>(),
            Array.Empty<PumpDefinition>(),
            suffixes.SelectMany(suffix => new[]
            {
                new ThermalBodyDefinition($"fuel-{suffix}", HeatCapacity.FromJoulesPerKelvin(1_000_000d)),
                new ThermalBodyDefinition($"structure-{suffix}", HeatCapacity.FromJoulesPerKelvin(2_000_000d)),
            }).ToArray(),
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());
}
