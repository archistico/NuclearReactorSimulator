using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Quantities;

public sealed class QuadraticHydraulicResistanceTests
{
    [Fact]
    public void StoresCanonicalSiValue()
    {
        var resistance = QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(125_000d);

        Assert.Equal(125_000d, resistance.PascalSecondsSquaredPerKilogramSquared, 12);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    public void NonPositiveResistance_IsRejected(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(value));
    }

    [Fact]
    public void NonFiniteResistance_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(double.NegativeInfinity));
    }
}
