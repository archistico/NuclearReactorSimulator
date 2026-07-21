using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Domain.Physics.Reactor.Feedback;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Reactor.Feedback;

public sealed class VoidFeedbackDomainTests
{
    [Fact]
    public void VoidFraction_ConvertsPercentAndSupportsSignedDifference()
    {
        var lower = VoidFraction.FromPercent(25d);
        var upper = VoidFraction.FromPercent(40d);

        var difference = upper - lower;

        Assert.Equal(0.4d, upper.Fraction, 12);
        Assert.Equal(40d, upper.Percent, 12);
        Assert.Equal(0.15d, difference.Fraction, 12);
        Assert.Equal(15d, difference.PercentagePoints, 12);
    }

    [Theory]
    [InlineData(-0.001d)]
    [InlineData(1.001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void VoidFraction_RejectsInvalidValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => VoidFraction.FromFraction(value));
    }

    [Fact]
    public void VoidReactivityCoefficient_ConvertsPcmPerPercentVoidAndMultipliesDifference()
    {
        var coefficient = VoidReactivityCoefficient.FromPcmPerPercentVoid(5d);
        var difference = VoidFractionDifference.FromPercentagePoints(10d);

        var reactivity = coefficient * difference;

        Assert.Equal(5d, coefficient.PcmPerPercentVoid, 12);
        Assert.Equal(50d, reactivity.Pcm, 9);
    }

    [Fact]
    public void VoidFeedbackDefinition_IsAlwaysVoidContribution()
    {
        var definition = new VoidReactivityFeedbackDefinition(
            "void/core",
            VoidFraction.FromPercent(15d),
            VoidReactivityCoefficient.FromPcmPerPercentVoid(2d));

        Assert.Equal("void/core", definition.Id);
        Assert.Equal(ReactivityContributionKind.Void, definition.Kind);
        Assert.Equal(15d, definition.ReferenceVoidFraction.Percent, 12);
    }

    [Fact]
    public void VoidFeedbackDefinition_RejectsBlankId()
    {
        Assert.Throws<ArgumentException>(() => new VoidReactivityFeedbackDefinition(
            " ",
            VoidFraction.NoVoid,
            VoidReactivityCoefficient.Zero));
    }
}
