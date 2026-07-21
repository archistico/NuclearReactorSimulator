using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics;

public sealed class DimensionalArithmeticTests
{
    [Fact]
    public void GeometryArithmetic_ProducesAreaAndVolume()
    {
        var width = Length.FromMetres(2d);
        var height = Length.FromMetres(3d);
        var depth = Length.FromMetres(4d);

        var area = width * height;
        var volume = area * depth;

        Assert.Equal(6d, area.SquareMetres, 12);
        Assert.Equal(24d, volume.CubicMetres, 12);
    }

    [Fact]
    public void MassAndVolume_ProduceDensityAndRoundTripMass()
    {
        var mass = Mass.FromKilograms(998d);
        var volume = Volume.FromCubicMetres(1d);

        var density = mass / volume;
        var reconstructedMass = density * volume;

        Assert.Equal(998d, density.KilogramsPerCubicMetre, 12);
        Assert.Equal(mass, reconstructedMass);
    }

    [Fact]
    public void ZeroVolume_CannotProduceDensity()
    {
        Assert.Throws<DivideByZeroException>(() => _ = Mass.FromKilograms(1d) / Volume.Zero);
    }

    [Fact]
    public void EnergyAndMass_ProduceSpecificEnergyAndRoundTripEnergy()
    {
        var energy = Energy.FromMegajoules(10d);
        var mass = Mass.FromKilograms(5d);

        var specificEnergy = energy / mass;
        var reconstructedEnergy = specificEnergy * mass;

        Assert.Equal(2_000d, specificEnergy.KilojoulesPerKilogram, 12);
        Assert.Equal(energy, reconstructedEnergy);
    }

    [Fact]
    public void ZeroMass_CannotProduceSpecificEnergy()
    {
        Assert.Throws<DivideByZeroException>(() => _ = Energy.FromJoules(1d) / Mass.Zero);
    }

    [Fact]
    public void PowerIntegratedOverDuration_ProducesEnergy()
    {
        var power = Power.FromMegawatts(2d);

        var energy = power.Over(TimeSpan.FromSeconds(30d));

        Assert.Equal(60d, energy.Megajoules, 12);
        Assert.Equal(power, energy.Per(TimeSpan.FromSeconds(30d)));
    }

    [Fact]
    public void PressureDifferenceTimesVolume_ProducesMechanicalEnergy()
    {
        var pressureDifference = PressureDifference.FromMegapascals(2d);
        var displacedVolume = Volume.FromCubicMetres(3d);

        var energy = pressureDifference * displacedVolume;

        Assert.Equal(6d, energy.Megajoules, 12);
    }

    [Fact]
    public void MassPerDuration_ProducesMassFlowRate()
    {
        var mass = Mass.FromKilograms(120d);

        var flow = mass.Per(TimeSpan.FromMinutes(2d));

        Assert.Equal(1d, flow.KilogramsPerSecond, 12);
    }
}
