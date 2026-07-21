using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics;

public sealed class UnitConversionTests
{
    [Fact]
    public void GeometryConversions_RoundTripThroughSi()
    {
        Assert.Equal(1.25d, Length.FromCentimetres(125d).Metres, 12);
        Assert.Equal(1_250d, Length.FromMetres(1.25d).Millimetres, 12);
        Assert.Equal(2.5d, Area.FromSquareCentimetres(25_000d).SquareMetres, 12);
        Assert.Equal(2_500_000d, Area.FromSquareMetres(2.5d).SquareMillimetres, 8);
        Assert.Equal(1.5d, Volume.FromLitres(1_500d).CubicMetres, 12);
        Assert.Equal(1_500d, Volume.FromCubicMetres(1.5d).Litres, 12);
    }

    [Fact]
    public void MassConversions_RoundTripThroughKilograms()
    {
        Assert.Equal(1.5d, Mass.FromGrams(1_500d).Kilograms, 12);
        Assert.Equal(1_500d, Mass.FromTonnes(1.5d).Kilograms, 12);
        Assert.Equal(1.5d, Mass.FromKilograms(1_500d).Tonnes, 12);
    }

    [Fact]
    public void TemperatureConversions_UseAbsoluteKelvinInternally()
    {
        var freezing = Temperature.FromDegreesCelsius(0d);
        var boiling = Temperature.FromDegreesCelsius(100d);

        Assert.Equal(273.15d, freezing.Kelvins, 12);
        Assert.Equal(373.15d, boiling.Kelvins, 12);
        Assert.Equal(100d, boiling.DegreesCelsius, 12);
        Assert.Equal(100d, (boiling - freezing).Kelvins, 12);
    }

    [Fact]
    public void PressureConversions_UseAbsolutePascalsInternally()
    {
        Assert.Equal(100_000d, Pressure.FromBar(1d).Pascals, 8);
        Assert.Equal(7_000_000d, Pressure.FromMegapascals(7d).Pascals, 8);
        Assert.Equal(101_325d, Pressure.StandardAtmosphere.Pascals, 8);
        Assert.Equal(1d, Pressure.StandardAtmosphere.StandardAtmospheres, 12);
        Assert.Equal(-200_000d, PressureDifference.FromBar(-2d).Pascals, 8);
    }

    [Fact]
    public void EnergyAndPowerConversions_UseJoulesAndWattsInternally()
    {
        Assert.Equal(3_600_000d, Energy.FromKilowattHours(1d).Joules, 8);
        Assert.Equal(3_600_000_000d, Energy.FromMegawattHours(1d).Joules, 4);
        Assert.Equal(2_500_000d, Power.FromMegawatts(2.5d).Watts, 8);
        Assert.Equal(1.5d, Power.FromWatts(1_500_000_000d).Gigawatts, 12);
        Assert.Equal(2_500_000d, SpecificEnergy.FromKilojoulesPerKilogram(2_500d).JoulesPerKilogram, 8);
    }

    [Fact]
    public void FlowConversions_UseSiPerSecondInternally()
    {
        Assert.Equal(1d, MassFlowRate.FromKilogramsPerHour(3_600d).KilogramsPerSecond, 12);
        Assert.Equal(3_600d, MassFlowRate.FromKilogramsPerSecond(1d).KilogramsPerHour, 12);
        Assert.Equal(0.001d, VolumetricFlowRate.FromLitresPerSecond(1d).CubicMetresPerSecond, 12);
        Assert.Equal(1d, VolumetricFlowRate.FromCubicMetresPerHour(3_600d).CubicMetresPerSecond, 12);
    }
}
