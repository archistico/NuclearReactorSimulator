using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Thermal;

public sealed class ThermalQuantityTests
{
    [Fact]
    public void HeatCapacity_ConvertsCanonicalAndEngineeringUnits()
    {
        var capacity = HeatCapacity.FromMegajoulesPerKelvin(2.5d);

        Assert.Equal(2_500_000d, capacity.JoulesPerKelvin, 12);
        Assert.Equal(2_500d, capacity.KilojoulesPerKelvin, 12);
        Assert.Equal(2.5d, capacity.MegajoulesPerKelvin, 12);
    }

    [Fact]
    public void ThermalConductance_ConvertsCanonicalAndEngineeringUnits()
    {
        var conductance = ThermalConductance.FromKilowattsPerKelvin(25d);

        Assert.Equal(25_000d, conductance.WattsPerKelvin, 12);
        Assert.Equal(25d, conductance.KilowattsPerKelvin, 12);
        Assert.Equal(0.025d, conductance.MegawattsPerKelvin, 12);
    }

    [Fact]
    public void HeatCapacityTimesTemperatureDifference_ProducesEnergy()
    {
        var energy = HeatCapacity.FromKilojoulesPerKelvin(20d)
            * TemperatureDifference.FromKelvins(15d);

        Assert.Equal(300d, energy.Kilojoules, 12);
    }

    [Fact]
    public void ConductanceTimesTemperatureDifference_ProducesSignedPower()
    {
        var conductance = ThermalConductance.FromKilowattsPerKelvin(10d);

        Assert.Equal(
            200d,
            (conductance * TemperatureDifference.FromKelvins(20d)).Kilowatts,
            12);
        Assert.Equal(
            -200d,
            (conductance * TemperatureDifference.FromKelvins(-20d)).Kilowatts,
            12);
    }
}
