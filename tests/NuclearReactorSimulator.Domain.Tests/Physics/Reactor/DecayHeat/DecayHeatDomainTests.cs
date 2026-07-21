using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.DecayHeat;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Reactor.DecayHeat;

public sealed class DecayHeatDomainTests
{
    [Fact]
    public void GenerationFraction_SupportsFractionAndPercentConversions()
    {
        var fraction = DecayHeatGenerationFraction.FromPercent(6.5d);

        Assert.Equal(0.065d, fraction.Fraction, 12);
        Assert.Equal(6.5d, fraction.Percent, 12);
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void GenerationFraction_RejectsInvalidValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DecayHeatGenerationFraction.FromFraction(value));
    }

    [Fact]
    public void Definition_CanonicalizesGroupsAndDestinations()
    {
        var definition = new DecayHeatDefinition(
            "core-decay",
            [
                Group("slow", 0.02d, 0.01d),
                Group("fast", 0.04d, 0.2d),
            ],
            [
                Destination("fuel", 0.8d),
                Destination("coolant", 0.2d),
            ]);

        Assert.Equal(new[] { "fast", "slow" }, definition.Groups.Select(static group => group.Id));
        Assert.Equal(new[] { "coolant", "fuel" }, definition.HeatDestinations.Select(static destination => destination.TargetDomainId));
        Assert.Equal(0.06d, definition.TotalGenerationFraction.Fraction, 12);
    }

    [Fact]
    public void Definition_RejectsGenerationFractionAboveUnity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DecayHeatDefinition(
            "invalid",
            [
                Group("a", 0.6d, 0.1d),
                Group("b", 0.5d, 0.2d),
            ],
            [Destination("fuel", 1d)]));
    }

    [Fact]
    public void Definition_RequiresCompleteDepositionPartition()
    {
        Assert.Throws<ArgumentException>(() => new DecayHeatDefinition(
            "invalid",
            [Group("a", 0.06d, 0.1d)],
            [
                Destination("fuel", 0.8d),
                Destination("coolant", 0.1d),
            ]));
    }

    [Fact]
    public void EquilibriumState_StoresFractionTimesPowerOverDecayConstant()
    {
        var definition = new DecayHeatDefinition(
            "core-decay",
            [Group("g", 0.06d, 0.1d)],
            [Destination("fuel", 1d)]);

        var state = DecayHeatState.CreateEquilibrium(definition, Power.FromMegawatts(1_000d));

        Assert.Equal(600_000_000d, state.GetGroup("g").StoredDecayEnergy.Joules, 3);
    }

    [Fact]
    public void EmptyState_CoversEveryConfiguredGroupInCanonicalOrder()
    {
        var definition = new DecayHeatDefinition(
            "core-decay",
            [
                Group("slow", 0.02d, 0.01d),
                Group("fast", 0.04d, 0.2d),
            ],
            [Destination("fuel", 1d)]);

        var state = DecayHeatState.CreateEmpty(definition);

        Assert.Equal(new[] { "fast", "slow" }, state.Groups.Select(static group => group.GroupId));
        Assert.All(state.Groups, static group => Assert.Equal(Energy.Zero, group.StoredDecayEnergy));
    }

    private static DecayHeatGroupDefinition Group(string id, double fraction, double lambda)
        => new(
            id,
            DecayHeatGenerationFraction.FromFraction(fraction),
            DecayConstant.FromPerSecond(lambda));

    private static DecayHeatDestinationDefinition Destination(string id, double fraction)
        => new(id, HeatDepositionFraction.FromFraction(fraction));
}
