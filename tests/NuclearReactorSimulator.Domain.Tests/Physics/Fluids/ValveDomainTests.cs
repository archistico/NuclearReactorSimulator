using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Fluids;

public sealed class ValveDomainTests
{
    private static readonly PipeDefinition Pipe = new(
        "pipe",
        "from",
        "to",
        QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000d));

    [Fact]
    public void ValvePosition_ConvertsFractionAndPercent()
    {
        var fromFraction = ValvePosition.FromFraction(0.25d);
        var fromPercent = ValvePosition.FromPercent(25d);

        Assert.Equal(fromFraction, fromPercent);
        Assert.Equal(25d, fromFraction.Percent, 12);
        Assert.False(fromFraction.IsClosed);
        Assert.False(fromFraction.IsFullyOpen);
    }

    [Fact]
    public void ValvePosition_BoundariesAreExact()
    {
        Assert.True(ValvePosition.Closed.IsClosed);
        Assert.Equal(0d, ValvePosition.Closed.Fraction);
        Assert.True(ValvePosition.FullyOpen.IsFullyOpen);
        Assert.Equal(1d, ValvePosition.FullyOpen.Fraction);
    }

    [Theory]
    [InlineData(-0.001d)]
    [InlineData(1.001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ValvePosition_InvalidFractionIsRejected(double fraction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ValvePosition.FromFraction(fraction));
    }

    [Fact]
    public void EqualPercentageCharacteristic_RequiresRangeabilityGreaterThanOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ValveCharacteristic.EqualPercentage(1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => ValveCharacteristic.EqualPercentage(double.NaN));
    }

    [Fact]
    public void ValveDefinition_NormalizesIdentifierAndPreservesConfiguration()
    {
        var characteristic = ValveCharacteristic.EqualPercentage(50d);
        var valve = new ValveDefinition(
            " feedwater-control ",
            Pipe,
            characteristic,
            ValveFailSafeAction.FailClosed);

        Assert.Equal("feedwater-control", valve.Id);
        Assert.Same(Pipe, valve.Pipe);
        Assert.Equal(characteristic, valve.Characteristic);
        Assert.Equal(ValveFailSafeAction.FailClosed, valve.FailSafeAction);
    }

    [Fact]
    public void ValveState_NormalizesIdentifierAndPreservesPosition()
    {
        var position = ValvePosition.FromPercent(42d);
        var state = new ValveState(" valve-1 ", position, true);

        Assert.Equal("valve-1", state.ValveId);
        Assert.Equal(position, state.Position);
        Assert.True(state.IsFailSafeActive);
    }

    [Fact]
    public void EmptyValveIdentifiers_AreRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new ValveDefinition(" ", Pipe, ValveCharacteristic.Linear, ValveFailSafeAction.FailClosed));
        Assert.Throws<ArgumentException>(() =>
            new ValveState(" ", ValvePosition.Closed));
    }

    [Fact]
    public void UnknownFailSafeAction_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ValveDefinition(
                "valve",
                Pipe,
                ValveCharacteristic.Linear,
                (ValveFailSafeAction)999));
    }
}
