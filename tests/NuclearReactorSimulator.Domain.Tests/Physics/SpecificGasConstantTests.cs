using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics;

public sealed class SpecificGasConstantTests
{
    [Fact]
    public void Quantity_ConvertsCanonicalJoulesAndKilojoulesPerKilogramKelvin()
    {
        var fromJoules = SpecificGasConstant.FromJoulesPerKilogramKelvin(461.526d);
        var fromKilojoules = SpecificGasConstant.FromKilojoulesPerKilogramKelvin(0.461526d);

        Assert.Equal(fromJoules, fromKilojoules);
        Assert.Equal(461.526d, fromJoules.JoulesPerKilogramKelvin, 12);
        Assert.Equal(0.461526d, fromJoules.KilojoulesPerKilogramKelvin, 12);
    }

    [Fact]
    public void Quantity_RejectsNegativeAndNonFiniteValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SpecificGasConstant.FromJoulesPerKilogramKelvin(-1d));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SpecificGasConstant.FromJoulesPerKilogramKelvin(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SpecificGasConstant.FromJoulesPerKilogramKelvin(double.PositiveInfinity));
    }
}
