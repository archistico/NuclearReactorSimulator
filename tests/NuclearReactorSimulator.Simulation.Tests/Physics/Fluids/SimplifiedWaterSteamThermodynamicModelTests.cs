using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class SimplifiedWaterSteamThermodynamicModelTests
{
    private readonly SimplifiedWaterSteamThermodynamicModel _model = new();

    [Fact]
    public void SaturationProperties_AtOneHundredCelsius_ArePhysicallyPlausible()
    {
        var properties = _model.GetSaturationProperties(Temperature.FromDegreesCelsius(100d));

        Assert.Equal(101.418d, properties.Pressure.Kilopascals, 2);
        Assert.InRange(properties.SaturatedLiquidDensity.KilogramsPerCubicMetre, 957d, 960d);
        Assert.InRange(properties.SaturatedVaporDensity.KilogramsPerCubicMetre, 0.59d, 0.61d);
        Assert.True(properties.SaturatedVaporInternalEnergy > properties.SaturatedLiquidInternalEnergy);
    }

    [Fact]
    public void SaturatedMixture_RoundTripsTemperaturePressureAndQuality()
    {
        var temperature = Temperature.FromDegreesCelsius(285d);
        var saturation = _model.GetSaturationProperties(temperature);
        const double expectedQuality = 0.25d;
        const double massKilograms = 1_000d;
        var specificVolume =
            ((1d - expectedQuality) * saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram)
            + (expectedQuality * saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram);
        var specificEnergy =
            ((1d - expectedQuality) * saturation.SaturatedLiquidInternalEnergy.JoulesPerKilogram)
            + (expectedQuality * saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram);
        var definition = new FluidNodeDefinition(
            "two-phase",
            Volume.FromCubicMetres(specificVolume * massKilograms));
        var inventory = new FluidNodeInventory(
            Mass.FromKilograms(massKilograms),
            Energy.FromJoules(specificEnergy * massKilograms));

        var result = _model.Resolve(definition, inventory, PreviousState());

        Assert.Equal(FluidPhase.SaturatedMixture, result.Phase);
        Assert.NotNull(result.VaporQuality);
        Assert.Equal(expectedQuality, result.VaporQuality!.Value.Fraction, 6);
        Assert.Equal(temperature.Kelvins, result.Temperature.Kelvins, 5);
        Assert.Equal(saturation.Pressure.Pascals, result.Pressure.Pascals, 1);
    }

    [Fact]
    public void DenseInventory_ResolvesAsSubcooledLiquid()
    {
        var temperature = Temperature.FromDegreesCelsius(250d);
        var saturation = _model.GetSaturationProperties(temperature);
        const double massKilograms = 1_000d;
        var density = saturation.SaturatedLiquidDensity.KilogramsPerCubicMetre * 1.002d;
        var definition = new FluidNodeDefinition(
            "liquid",
            Volume.FromCubicMetres(massKilograms / density));
        var inventory = new FluidNodeInventory(
            Mass.FromKilograms(massKilograms),
            Energy.FromJoules(saturation.SaturatedLiquidInternalEnergy.JoulesPerKilogram * massKilograms));

        var result = _model.Resolve(definition, inventory, PreviousState());

        Assert.Equal(FluidPhase.SubcooledLiquid, result.Phase);
        Assert.Null(result.VaporQuality);
        Assert.Equal(0d, result.VaporMassFraction!.Value, 12);
        Assert.Equal(temperature.Kelvins, result.Temperature.Kelvins, 8);
        Assert.True(result.Pressure > saturation.Pressure);
    }

    [Fact]
    public void LowDensityHighEnergyInventory_ResolvesAsSuperheatedVapor()
    {
        var definition = new FluidNodeDefinition("vapor", Volume.FromCubicMetres(10d));
        var inventory = new FluidNodeInventory(
            Mass.FromKilograms(10d),
            Energy.FromMegajoules(30d));

        var result = _model.Resolve(definition, inventory, PreviousState());

        Assert.Equal(FluidPhase.SuperheatedVapor, result.Phase);
        Assert.Null(result.VaporQuality);
        Assert.Equal(1d, result.VaporMassFraction!.Value, 12);
        Assert.True(result.Temperature > Temperature.FromDegreesCelsius(100d));
        Assert.True(result.Pressure > Pressure.Vacuum);
    }

    [Fact]
    public void NegativeSpecificInternalEnergy_IsRejectedAsOutOfRange()
    {
        var definition = new FluidNodeDefinition("invalid", Volume.FromCubicMetres(1d));
        var inventory = new FluidNodeInventory(
            Mass.FromKilograms(1_000d),
            Energy.FromMegajoules(-10d));

        var exception = Assert.Throws<WaterSteamStateOutOfRangeException>(() =>
            _model.Resolve(definition, inventory, PreviousState()));

        Assert.Equal("invalid", exception.NodeId);
    }

    [Fact]
    public void SameConservedState_ProducesSameThermodynamicState()
    {
        var saturation = _model.GetSaturationProperties(Temperature.FromDegreesCelsius(200d));
        const double quality = 0.4d;
        const double mass = 500d;
        var specificVolume =
            ((1d - quality) * saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram)
            + (quality * saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram);
        var specificEnergy =
            ((1d - quality) * saturation.SaturatedLiquidInternalEnergy.JoulesPerKilogram)
            + (quality * saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram);
        var definition = new FluidNodeDefinition("repeatable", Volume.FromCubicMetres(specificVolume * mass));
        var inventory = new FluidNodeInventory(Mass.FromKilograms(mass), Energy.FromJoules(specificEnergy * mass));

        var left = _model.Resolve(definition, inventory, PreviousState());
        var right = _model.Resolve(definition, inventory, new FluidThermodynamicState(
            Pressure.FromKilopascals(50d),
            Temperature.FromDegreesCelsius(20d)));

        Assert.Equal(left, right);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(700d)]
    public void SaturationProperties_RejectOutOfRangeTemperature(double degreesCelsius)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _model.GetSaturationProperties(Temperature.FromDegreesCelsius(degreesCelsius)));
    }

    private static FluidThermodynamicState PreviousState()
    {
        return new FluidThermodynamicState(
            Pressure.StandardAtmosphere,
            Temperature.FromDegreesCelsius(20d));
    }
}
