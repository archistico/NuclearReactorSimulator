using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Reactor.Core.Channels;

public sealed class FuelChannelGroupSetDefinitionTests
{
    [Fact]
    public void Constructor_CanonicalizesGroupsAndCountsRepresentedChannels()
    {
        var fixture = CreateFixture();
        var definition = new FuelChannelGroupSetDefinition(
            "channels",
            fixture.Core,
            new[]
            {
                Group("group-b", "pipe-b", "inlet-b", 6, 60),
                Group("group-a", "pipe-a", "inlet-a", 4, 40),
            });

        Assert.Equal(new[] { "group-a", "group-b" }, definition.Groups.Select(static group => group.Id));
        Assert.Equal(10, definition.RepresentedChannelCount);
    }

    [Fact]
    public void Constructor_RejectsZonePowerFractionsThatDoNotClose()
    {
        var fixture = CreateFixture();

        Assert.Throws<ArgumentException>(() => new FuelChannelGroupSetDefinition(
            "channels",
            fixture.Core,
            new[]
            {
                Group("group-a", "pipe-a", "inlet-a", 4, 40),
                Group("group-b", "pipe-b", "inlet-b", 6, 50),
            }));
    }

    [Fact]
    public void Constructor_RejectsHydraulicPipeWhoseEndpointsDoNotMatchGroup()
    {
        var fixture = CreateFixture();
        var invalid = new FuelChannelGroupDefinition(
            "group-a",
            "zone-a",
            4,
            CoreZonePowerFraction.Full,
            "pipe-a",
            "inlet-b",
            "outlet",
            "fuel",
            "structure",
            HeatDepositionFraction.FromPercent(70),
            HeatDepositionFraction.FromPercent(10),
            HeatDepositionFraction.FromPercent(20));

        Assert.Throws<ArgumentException>(() => new FuelChannelGroupSetDefinition("channels", fixture.Core, new[] { invalid }));
    }

    [Fact]
    public void Constructor_RejectsGroupDomainsThatDoNotMatchParentZone()
    {
        var fixture = CreateFixture(includeAlternateFuel: true);
        var invalid = new FuelChannelGroupDefinition(
            "group-a",
            "zone-a",
            4,
            CoreZonePowerFraction.Full,
            "pipe-a",
            "inlet-a",
            "outlet",
            "alternate-fuel",
            "structure",
            HeatDepositionFraction.FromPercent(70),
            HeatDepositionFraction.FromPercent(10),
            HeatDepositionFraction.FromPercent(20));

        Assert.Throws<ArgumentException>(() => new FuelChannelGroupSetDefinition("channels", fixture.Core, new[] { invalid }));
    }

    [Fact]
    public void GroupDefinition_RejectsHeatFractionsThatDoNotClose()
    {
        Assert.Throws<ArgumentException>(() => new FuelChannelGroupDefinition(
            "group-a",
            "zone-a",
            4,
            CoreZonePowerFraction.Full,
            "pipe-a",
            "inlet-a",
            "outlet",
            "fuel",
            "structure",
            HeatDepositionFraction.FromPercent(70),
            HeatDepositionFraction.FromPercent(10),
            HeatDepositionFraction.FromPercent(10)));
    }

    private static FuelChannelGroupDefinition Group(
        string id,
        string pipeId,
        string inletId,
        int channelCount,
        double zonePercent)
        => new(
            id,
            "zone-a",
            channelCount,
            CoreZonePowerFraction.FromPercent(zonePercent),
            pipeId,
            inletId,
            "outlet",
            "fuel",
            "structure",
            HeatDepositionFraction.FromPercent(70),
            HeatDepositionFraction.FromPercent(10),
            HeatDepositionFraction.FromPercent(20));

    private static Fixture CreateFixture(bool includeAlternateFuel = false)
    {
        var thermalBodies = new List<ThermalBodyDefinition>
        {
            new("fuel", HeatCapacity.FromJoulesPerKelvin(1_000_000)),
            new("structure", HeatCapacity.FromJoulesPerKelvin(1_000_000)),
        };
        if (includeAlternateFuel)
        {
            thermalBodies.Add(new ThermalBodyDefinition("alternate-fuel", HeatCapacity.FromJoulesPerKelvin(1_000_000)));
        }

        var plant = new PlantDefinition(
            "plant",
            new[]
            {
                new FluidNodeDefinition("inlet-a", Volume.FromCubicMetres(10)),
                new FluidNodeDefinition("inlet-b", Volume.FromCubicMetres(10)),
                new FluidNodeDefinition("outlet", Volume.FromCubicMetres(10)),
            },
            new[]
            {
                new PipeDefinition("pipe-a", "inlet-a", "outlet", QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000)),
                new PipeDefinition("pipe-b", "inlet-b", "outlet", QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000)),
            },
            Array.Empty<ValveDefinition>(),
            Array.Empty<PumpDefinition>(),
            thermalBodies,
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());
        var core = AggregatedCoreDefinition.CreateSingleZone("core", plant, "zone-a", "fuel", "structure", "outlet");
        return new Fixture(core);
    }

    private sealed record Fixture(AggregatedCoreDefinition Core);
}
