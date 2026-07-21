using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.Feedback;

public sealed class WaterSteamVoidFractionSolverTests
{
    private readonly SimplifiedWaterSteamThermodynamicModel _thermodynamicModel = new();

    [Fact]
    public void SubcooledLiquid_ResolvesZeroVoid()
    {
        var solver = new WaterSteamVoidFractionSolver(_thermodynamicModel);
        var state = new FluidThermodynamicState(
            Pressure.FromMegapascals(5d),
            Temperature.FromDegreesCelsius(200d),
            FluidPhase.SubcooledLiquid,
            null);

        Assert.Equal(VoidFraction.NoVoid, solver.Resolve(state));
    }

    [Fact]
    public void SuperheatedVapor_ResolvesFullVoid()
    {
        var solver = new WaterSteamVoidFractionSolver(_thermodynamicModel);
        var state = new FluidThermodynamicState(
            Pressure.FromKilopascals(250d),
            Temperature.FromDegreesCelsius(220d),
            FluidPhase.SuperheatedVapor,
            null);

        Assert.Equal(VoidFraction.AllVapor, solver.Resolve(state));
    }

    [Fact]
    public void SaturatedMixture_ConvertsMassQualityToVolumetricVoidFraction()
    {
        var solver = new WaterSteamVoidFractionSolver(_thermodynamicModel);
        var temperature = Temperature.FromDegreesCelsius(150d);
        var saturation = _thermodynamicModel.GetSaturationProperties(temperature);
        const double quality = 0.10d;
        var expected = (quality / saturation.SaturatedVaporDensity.KilogramsPerCubicMetre)
            / ((quality / saturation.SaturatedVaporDensity.KilogramsPerCubicMetre)
               + ((1d - quality) / saturation.SaturatedLiquidDensity.KilogramsPerCubicMetre));
        var state = new FluidThermodynamicState(
            saturation.Pressure,
            temperature,
            FluidPhase.SaturatedMixture,
            VaporQuality.FromFraction(quality));

        var actual = solver.Resolve(state);

        Assert.Equal(expected, actual.Fraction, 12);
        Assert.True(actual.Fraction > quality);
    }

    [Fact]
    public void SaturatedEndpoints_ResolveExactVoidEndpoints()
    {
        var solver = new WaterSteamVoidFractionSolver(_thermodynamicModel);
        var saturation = _thermodynamicModel.GetSaturationProperties(Temperature.FromDegreesCelsius(120d));

        var liquid = new FluidThermodynamicState(
            saturation.Pressure,
            saturation.Temperature,
            FluidPhase.SaturatedMixture,
            VaporQuality.SaturatedLiquid);
        var vapor = new FluidThermodynamicState(
            saturation.Pressure,
            saturation.Temperature,
            FluidPhase.SaturatedMixture,
            VaporQuality.DrySaturatedVapor);

        Assert.Equal(VoidFraction.NoVoid, solver.Resolve(liquid));
        Assert.Equal(VoidFraction.AllVapor, solver.Resolve(vapor));
    }

    [Fact]
    public void UnspecifiedPhase_FailsFast()
    {
        var solver = new WaterSteamVoidFractionSolver(_thermodynamicModel);
        var state = new FluidThermodynamicState(
            Pressure.FromBar(10d),
            Temperature.FromDegreesCelsius(150d));

        Assert.Throws<InvalidOperationException>(() => solver.Resolve(state));
    }
}
