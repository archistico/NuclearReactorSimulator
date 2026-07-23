using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.TurbineIsland.Turbine;

public sealed class TurbineThermodynamicWorkDefinitionTests
{
    [Fact]
    public void Definition_PublishesValidatedEducationalVaporExpansionParameters()
    {
        var definition = new TurbineThermodynamicWorkDefinition(
            SpecificHeatCapacity.FromKilojoulesPerKilogramKelvin(2.1d),
            heatCapacityRatio: 1.3d,
            maximumInletInternalEnergyExtractionFraction: 0.8d);

        Assert.Equal(2.1d, definition.VaporSpecificHeatAtConstantPressure.KilojoulesPerKilogramKelvin, 12);
        Assert.Equal(1.3d, definition.HeatCapacityRatio, 12);
        Assert.Equal(0.8d, definition.MaximumInletInternalEnergyExtractionFraction, 12);
    }

    [Fact]
    public void Definition_RejectsInvalidHeatCapacityRatioAndExtractionFraction()
    {
        var heatCapacity = SpecificHeatCapacity.FromKilojoulesPerKilogramKelvin(2.1d);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurbineThermodynamicWorkDefinition(heatCapacity, 1d, 0.8d));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurbineThermodynamicWorkDefinition(heatCapacity, 1.3d, 0d));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurbineThermodynamicWorkDefinition(heatCapacity, 1.3d, 0.81d));
    }
}
