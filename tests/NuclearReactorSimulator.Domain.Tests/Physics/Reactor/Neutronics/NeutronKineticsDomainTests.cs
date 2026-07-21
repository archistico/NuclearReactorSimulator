using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Reactor.Neutronics;

public sealed class NeutronKineticsDomainTests
{
    [Fact]
    public void DelayedNeutronFraction_UsesExplicitDimensionlessAndPercentViews()
    {
        var fraction = DelayedNeutronFraction.FromPercent(0.65d);

        Assert.Equal(0.0065d, fraction.Fraction, 12);
        Assert.Equal(0.65d, fraction.Percent, 12);
    }

    [Theory]
    [InlineData(-0.001d)]
    [InlineData(1.001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void DelayedNeutronFraction_RejectsInvalidValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DelayedNeutronFraction.FromFraction(value));
    }

    [Fact]
    public void DecayConstant_CanBeConstructedFromHalfLife()
    {
        var decay = DecayConstant.FromHalfLife(TimeSpan.FromSeconds(10d));

        Assert.Equal(10d, decay.HalfLifeSeconds, 10);
        Assert.Equal(Math.Log(2d) / 10d, decay.PerSecond, 12);
    }

    [Fact]
    public void KineticsRatesAndPopulations_RejectInvalidValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DecayConstant.FromPerSecond(0d));
        Assert.Throws<ArgumentOutOfRangeException>(() => DecayConstant.FromPerSecond(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => NeutronPopulation.FromRelative(-0.001d));
        Assert.Throws<ArgumentOutOfRangeException>(() => NeutronPopulation.FromRelative(double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => DelayedNeutronPrecursorPopulation.FromRelative(-0.001d));
    }

    [Fact]
    public void Parameters_CanonicalizeGroupsAndComputeEffectiveBeta()
    {
        var parameters = new PointKineticsParameters(
            TimeSpan.FromMilliseconds(5d),
            [
                Group("slow", 0.0025d, 0.08d),
                Group("fast", 0.004d, 0.8d),
            ]);

        Assert.Equal(new[] { "fast", "slow" }, parameters.DelayedNeutronGroups.Select(static group => group.Id));
        Assert.Equal(0.0065d, parameters.EffectiveDelayedNeutronFraction.Fraction, 12);
        Assert.Equal(0.005d, parameters.PromptNeutronGenerationTimeSeconds, 12);
    }

    [Fact]
    public void Parameters_RejectDuplicateGroupIdsAndInvalidPromptLifetime()
    {
        Assert.Throws<ArgumentException>(() => new PointKineticsParameters(
            TimeSpan.FromMilliseconds(5d),
            [Group("g", 0.003d, 0.1d), Group("g", 0.003d, 0.2d)]));

        Assert.Throws<ArgumentOutOfRangeException>(() => new PointKineticsParameters(
            TimeSpan.Zero,
            [Group("g", 0.0065d, 0.1d)]));
    }

    [Fact]
    public void CriticalEquilibrium_InitializesPrecursorPopulationsFromParameterSet()
    {
        var parameters = Parameters();
        var state = PointKineticsState.CreateCriticalEquilibrium(parameters, NeutronPopulation.Reference);

        Assert.Equal(1d, state.NeutronPopulation.Relative, 12);
        Assert.Equal(10d, state.GetGroup("slow").PrecursorPopulation.Relative, 12);
        Assert.Equal(0.625d, state.GetGroup("fast").PrecursorPopulation.Relative, 12);
    }

    [Fact]
    public void PointKineticsState_IsCanonicalAndIndependentFromCallerArray()
    {
        var source = new[]
        {
            new DelayedNeutronGroupState("slow", DelayedNeutronPrecursorPopulation.FromRelative(2d)),
            new DelayedNeutronGroupState("fast", DelayedNeutronPrecursorPopulation.FromRelative(1d)),
        };
        var state = new PointKineticsState(NeutronPopulation.Reference, source);

        source[0] = new DelayedNeutronGroupState("replacement", DelayedNeutronPrecursorPopulation.Zero);

        Assert.Equal(new[] { "fast", "slow" }, state.DelayedNeutronGroups.Select(static group => group.GroupId));
        Assert.Equal(2d, state.GetGroup("slow").PrecursorPopulation.Relative, 12);
    }

    private static PointKineticsParameters Parameters()
        => new(
            TimeSpan.FromMilliseconds(5d),
            [
                Group("slow", 0.004d, 0.08d),
                Group("fast", 0.0025d, 0.8d),
            ]);

    private static DelayedNeutronGroupDefinition Group(string id, double beta, double lambda)
        => new(
            id,
            DelayedNeutronFraction.FromFraction(beta),
            DecayConstant.FromPerSecond(lambda));
}
