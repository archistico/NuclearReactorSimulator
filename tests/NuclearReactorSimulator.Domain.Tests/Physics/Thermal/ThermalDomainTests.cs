using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Thermal;

public sealed class ThermalDomainTests
{
    [Fact]
    public void ThermalBodyState_FromTemperatureStoresEnergyAndDerivesSameTemperature()
    {
        var definition = new ThermalBodyDefinition(
            "wall",
            HeatCapacity.FromMegajoulesPerKelvin(10d));

        var state = ThermalBodyState.FromTemperature(
            definition,
            Temperature.FromDegreesCelsius(300d));

        Assert.Equal(5_731.5d, state.StoredThermalEnergy.Megajoules, 9);
        Assert.Equal(300d, state.Temperature.DegreesCelsius, 12);
    }

    [Fact]
    public void ThermalBodyDefinition_RequiresPositiveHeatCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ThermalBodyDefinition("wall", HeatCapacity.Zero));
    }

    [Fact]
    public void ThermalBodyState_RejectsNegativeStoredEnergy()
    {
        var definition = new ThermalBodyDefinition(
            "wall",
            HeatCapacity.FromKilojoulesPerKelvin(100d));

        Assert.Throws<ArgumentOutOfRangeException>(() => new ThermalBodyState(
            definition,
            Energy.FromJoules(-1d)));
    }

    [Fact]
    public void HeatTransferDefinition_RequiresDistinctEndpointsAndPositiveConductance()
    {
        Assert.Throws<ArgumentException>(() => new HeatTransferDefinition(
            "link",
            "same",
            "same",
            ThermalConductance.FromWattsPerKelvin(1d)));

        Assert.Throws<ArgumentOutOfRangeException>(() => new HeatTransferDefinition(
            "link",
            "a",
            "b",
            ThermalConductance.Zero));
    }

    [Fact]
    public void HeatSourceDefinition_RejectsNegativeRatedPower()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HeatSourceDefinition(
            "heater",
            "wall",
            Power.FromWatts(-1d)));
    }
}
