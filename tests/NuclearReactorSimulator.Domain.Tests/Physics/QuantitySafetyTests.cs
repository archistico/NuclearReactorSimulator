using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics;

public sealed class QuantitySafetyTests
{
    [Fact]
    public void NonNegativeQuantities_RejectArithmeticThatWouldProduceNegativeResult()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Mass.FromKilograms(1d) - Mass.FromKilograms(2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Volume.FromCubicMetres(1d) - Volume.FromCubicMetres(2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Length.FromMetres(1d) * -1d);
    }

    [Fact]
    public void ScalarDivision_RejectsZeroOrNegativeDivisors()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Power.FromWatts(1d) / 0d);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Energy.FromJoules(1d) / -1d);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = MassFlowRate.FromKilogramsPerSecond(1d) / 0d);
    }

    [Fact]
    public void PerOperations_RejectNonPositiveDurations()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Mass.FromKilograms(1d).Per(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => Energy.FromJoules(1d).Per(TimeSpan.FromSeconds(-1d)));
    }

    [Fact]
    public void PowerIntegration_RejectsNegativeDurationButAcceptsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Power.FromWatts(1d).Over(TimeSpan.FromSeconds(-1d)));
        Assert.Equal(Energy.Zero, Power.FromWatts(1d).Over(TimeSpan.Zero));
    }

    [Fact]
    public void Quantities_AreValueTypesWithValueEquality()
    {
        var first = Pressure.FromMegapascals(7d);
        var second = Pressure.FromPascals(7_000_000d);

        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void Comparisons_OperateOnCanonicalSiValues()
    {
        Assert.True(Pressure.FromBar(2d) > Pressure.FromKilopascals(150d));
        Assert.True(Temperature.FromDegreesCelsius(100d) > Temperature.FromKelvins(300d));
        Assert.True(Mass.FromTonnes(1d) >= Mass.FromKilograms(1_000d));
        Assert.True(Power.FromMegawatts(-1d) < Power.Zero);
    }
}
