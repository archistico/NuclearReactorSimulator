using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Fluids;

public sealed class FluidPhaseDomainTests
{
    [Fact]
    public void VaporQuality_SupportsFractionAndPercentFactories()
    {
        var fromFraction = VaporQuality.FromFraction(0.35d);
        var fromPercent = VaporQuality.FromPercent(35d);

        Assert.Equal(fromFraction, fromPercent);
        Assert.Equal(0.35d, fromFraction.Fraction, 12);
        Assert.Equal(35d, fromFraction.Percent, 12);
    }

    [Theory]
    [InlineData(-0.0001d)]
    [InlineData(1.0001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void VaporQuality_RejectsInvalidValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => VaporQuality.FromFraction(value));
    }

    [Fact]
    public void SaturatedMixture_RequiresVaporQuality()
    {
        Assert.Throws<ArgumentException>(() => new FluidThermodynamicState(
            Pressure.StandardAtmosphere,
            Temperature.FromDegreesCelsius(100d),
            FluidPhase.SaturatedMixture,
            null));
    }

    [Fact]
    public void NonMixturePhase_RejectsVaporQuality()
    {
        Assert.Throws<ArgumentException>(() => new FluidThermodynamicState(
            Pressure.StandardAtmosphere,
            Temperature.FromDegreesCelsius(20d),
            FluidPhase.SubcooledLiquid,
            VaporQuality.SaturatedLiquid));
    }

    [Fact]
    public void PhaseState_ExposesDerivedVaporMassFraction()
    {
        var liquid = new FluidThermodynamicState(
            Pressure.StandardAtmosphere,
            Temperature.FromDegreesCelsius(20d),
            FluidPhase.SubcooledLiquid,
            null);
        var mixture = new FluidThermodynamicState(
            Pressure.StandardAtmosphere,
            Temperature.FromDegreesCelsius(100d),
            FluidPhase.SaturatedMixture,
            VaporQuality.FromPercent(25d));
        var vapor = new FluidThermodynamicState(
            Pressure.StandardAtmosphere,
            Temperature.FromDegreesCelsius(180d),
            FluidPhase.SuperheatedVapor,
            null);

        Assert.Equal(0d, liquid.VaporMassFraction!.Value, 12);
        Assert.Equal(0.25d, mixture.VaporMassFraction!.Value, 12);
        Assert.Equal(1d, vapor.VaporMassFraction!.Value, 12);
    }

    [Fact]
    public void LegacyConstructor_RemainsPhaseUnspecified()
    {
        var state = new FluidThermodynamicState(
            Pressure.FromMegapascals(5d),
            Temperature.FromDegreesCelsius(250d));

        Assert.Equal(FluidPhase.Unspecified, state.Phase);
        Assert.Null(state.VaporQuality);
        Assert.Null(state.VaporMassFraction);
    }
}
