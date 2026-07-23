using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Quantities;

public sealed class SpecificHeatCapacityTests
{
    [Fact]
    public void SpecificHeatCapacity_ConvertsUnitsAndProducesSpecificEnergyAtAbsoluteTemperature()
    {
        var capacity = SpecificHeatCapacity.FromKilojoulesPerKilogramKelvin(2.1d);
        var energy = capacity * Temperature.FromKelvins(500d);

        Assert.Equal(2_100d, capacity.JoulesPerKilogramKelvin, 12);
        Assert.Equal(1_050d, energy.KilojoulesPerKilogram, 12);
    }

    [Fact]
    public void SpecificHeatCapacity_RejectsNegativeAndNonFiniteValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SpecificHeatCapacity.FromJoulesPerKilogramKelvin(-1d));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SpecificHeatCapacity.FromJoulesPerKilogramKelvin(double.NaN));
    }
}
