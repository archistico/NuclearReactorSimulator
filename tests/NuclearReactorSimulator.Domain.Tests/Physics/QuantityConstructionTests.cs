using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics;

public sealed class QuantityConstructionTests
{
    [Fact]
    public void AbsoluteQuantities_RejectNegativeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Length.FromMetres(-1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => Area.FromSquareMetres(-1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => Volume.FromCubicMetres(-1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => Mass.FromKilograms(-1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => Density.FromKilogramsPerCubicMetre(-1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => Temperature.FromKelvins(-1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => Pressure.FromPascals(-1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => HeatCapacity.FromJoulesPerKelvin(-1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => ThermalConductance.FromWattsPerKelvin(-1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => AngularSpeed.FromRadiansPerSecond(-1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => MomentOfInertia.FromKilogramSquareMetres(-1d));
    }

    [Fact]
    public void SignedQuantities_AcceptNegativeValues()
    {
        Assert.Equal(-5d, TemperatureDifference.FromKelvins(-5d).Kelvins);
        Assert.Equal(-5d, PressureDifference.FromPascals(-5d).Pascals);
        Assert.Equal(-5d, Energy.FromJoules(-5d).Joules);
        Assert.Equal(-5d, SpecificEnergy.FromJoulesPerKilogram(-5d).JoulesPerKilogram);
        Assert.Equal(-5d, Power.FromWatts(-5d).Watts);
        Assert.Equal(-5d, MassFlowRate.FromKilogramsPerSecond(-5d).KilogramsPerSecond);
        Assert.Equal(-5d, VolumetricFlowRate.FromCubicMetresPerSecond(-5d).CubicMetresPerSecond);
        Assert.Equal(-5d, Torque.FromNewtonMetres(-5d).NewtonMetres);
    }

    [Fact]
    public void EveryQuantity_RejectsNonFiniteValues()
    {
        Action<double>[] factories =
        [
            value => _ = Length.FromMetres(value),
            value => _ = Area.FromSquareMetres(value),
            value => _ = Volume.FromCubicMetres(value),
            value => _ = Mass.FromKilograms(value),
            value => _ = Density.FromKilogramsPerCubicMetre(value),
            value => _ = Temperature.FromKelvins(value),
            value => _ = TemperatureDifference.FromKelvins(value),
            value => _ = Pressure.FromPascals(value),
            value => _ = PressureDifference.FromPascals(value),
            value => _ = Energy.FromJoules(value),
            value => _ = SpecificEnergy.FromJoulesPerKilogram(value),
            value => _ = Power.FromWatts(value),
            value => _ = MassFlowRate.FromKilogramsPerSecond(value),
            value => _ = VolumetricFlowRate.FromCubicMetresPerSecond(value),
            value => _ = HeatCapacity.FromJoulesPerKelvin(value),
            value => _ = ThermalConductance.FromWattsPerKelvin(value),
            value => _ = AngularSpeed.FromRadiansPerSecond(value),
            value => _ = Torque.FromNewtonMetres(value),
            value => _ = MomentOfInertia.FromKilogramSquareMetres(value),
        ];

        double[] invalidValues = [double.NaN, double.PositiveInfinity, double.NegativeInfinity];

        foreach (var factory in factories)
        {
            foreach (var invalidValue in invalidValues)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => factory(invalidValue));
            }
        }
    }

    [Fact]
    public void DefaultValues_AreValidPhysicalZeros()
    {
        Assert.Equal(Length.Zero, default(Length));
        Assert.Equal(Area.Zero, default(Area));
        Assert.Equal(Volume.Zero, default(Volume));
        Assert.Equal(Mass.Zero, default(Mass));
        Assert.Equal(Density.Zero, default(Density));
        Assert.Equal(Temperature.AbsoluteZero, default(Temperature));
        Assert.Equal(TemperatureDifference.Zero, default(TemperatureDifference));
        Assert.Equal(Pressure.Vacuum, default(Pressure));
        Assert.Equal(PressureDifference.Zero, default(PressureDifference));
        Assert.Equal(Energy.Zero, default(Energy));
        Assert.Equal(SpecificEnergy.Zero, default(SpecificEnergy));
        Assert.Equal(Power.Zero, default(Power));
        Assert.Equal(MassFlowRate.Zero, default(MassFlowRate));
        Assert.Equal(VolumetricFlowRate.Zero, default(VolumetricFlowRate));
        Assert.Equal(HeatCapacity.Zero, default(HeatCapacity));
        Assert.Equal(ThermalConductance.Zero, default(ThermalConductance));
        Assert.Equal(AngularSpeed.Zero, default(AngularSpeed));
        Assert.Equal(Torque.Zero, default(Torque));
        Assert.Equal(MomentOfInertia.Zero, default(MomentOfInertia));
    }
}
