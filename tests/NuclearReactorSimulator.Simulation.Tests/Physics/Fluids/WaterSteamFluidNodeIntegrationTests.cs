using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class WaterSteamFluidNodeIntegrationTests
{
    [Fact]
    public void ZeroBalance_ClosesPreviouslyUnspecifiedStateIntoWaterSteamPhase()
    {
        var model = new SimplifiedWaterSteamThermodynamicModel();
        var saturation = model.GetSaturationProperties(Temperature.FromDegreesCelsius(100d));
        const double quality = 0.2d;
        const double mass = 100d;
        var specificVolume =
            ((1d - quality) * saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram)
            + (quality * saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram);
        var specificEnergy =
            ((1d - quality) * saturation.SaturatedLiquidInternalEnergy.JoulesPerKilogram)
            + (quality * saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram);
        var initial = new FluidNodeState(
            new FluidNodeDefinition("boiling-node", Volume.FromCubicMetres(specificVolume * mass)),
            new FluidNodeInventory(Mass.FromKilograms(mass), Energy.FromJoules(specificEnergy * mass)),
            new FluidThermodynamicState(
                Pressure.FromMegapascals(5d),
                Temperature.FromDegreesCelsius(250d)));
        var integrator = new FluidNodeIntegrator(model);

        var result = integrator.Step(initial, FluidNodeBalance.Zero, TimeSpan.FromMilliseconds(20d));

        Assert.Equal(initial.Mass, result.Mass);
        Assert.Equal(initial.InternalEnergy, result.InternalEnergy);
        Assert.Equal(FluidPhase.SaturatedMixture, result.Phase);
        Assert.NotNull(result.VaporQuality);
        Assert.Equal(quality, result.VaporQuality!.Value.Fraction, 6);
        Assert.Equal(saturation.Pressure.Pascals, result.Pressure.Pascals, 1);
    }

    [Fact]
    public void AddedHeat_ChangesThermodynamicClosureWithoutChangingMass()
    {
        var model = new SimplifiedWaterSteamThermodynamicModel();
        var saturation = model.GetSaturationProperties(Temperature.FromDegreesCelsius(200d));
        const double quality = 0.3d;
        const double mass = 1_000d;
        var specificVolume =
            ((1d - quality) * saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram)
            + (quality * saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram);
        var specificEnergy =
            ((1d - quality) * saturation.SaturatedLiquidInternalEnergy.JoulesPerKilogram)
            + (quality * saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram);
        var initialThermodynamics = new FluidThermodynamicState(
            saturation.Pressure,
            saturation.Temperature,
            FluidPhase.SaturatedMixture,
            VaporQuality.FromFraction(quality));
        var initial = new FluidNodeState(
            new FluidNodeDefinition("heated-node", Volume.FromCubicMetres(specificVolume * mass)),
            new FluidNodeInventory(Mass.FromKilograms(mass), Energy.FromJoules(specificEnergy * mass)),
            initialThermodynamics);
        var integrator = new FluidNodeIntegrator(model);

        var result = integrator.Step(
            initial,
            new FluidNodeBalance(MassFlowRate.Zero, Power.FromMegawatts(1d)),
            TimeSpan.FromSeconds(1d));

        Assert.Equal(initial.Mass, result.Mass);
        Assert.Equal(initial.InternalEnergy.Joules + 1_000_000d, result.InternalEnergy.Joules, 6);
        Assert.NotEqual(initial.Thermodynamics, result.Thermodynamics);
        Assert.True(result.Pressure > Pressure.Vacuum);
        Assert.True(result.Temperature > Temperature.AbsoluteZero);
    }

    [Fact]
    public void RepeatedFixedSteps_AreDeterministic()
    {
        var left = CreateStableTwoPhaseState();
        var right = CreateStableTwoPhaseState();
        var leftIntegrator = new FluidNodeIntegrator(new SimplifiedWaterSteamThermodynamicModel());
        var rightIntegrator = new FluidNodeIntegrator(new SimplifiedWaterSteamThermodynamicModel());
        var balance = new FluidNodeBalance(MassFlowRate.Zero, Power.FromKilowatts(20d));

        for (var step = 0; step < 100; step++)
        {
            left = leftIntegrator.Step(left, balance, TimeSpan.FromMilliseconds(20d));
            right = rightIntegrator.Step(right, balance, TimeSpan.FromMilliseconds(20d));
        }

        Assert.Equal(left, right);
    }

    private static FluidNodeState CreateStableTwoPhaseState()
    {
        var model = new SimplifiedWaterSteamThermodynamicModel();
        var saturation = model.GetSaturationProperties(Temperature.FromDegreesCelsius(150d));
        const double quality = 0.15d;
        const double mass = 500d;
        var specificVolume =
            ((1d - quality) * saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram)
            + (quality * saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram);
        var specificEnergy =
            ((1d - quality) * saturation.SaturatedLiquidInternalEnergy.JoulesPerKilogram)
            + (quality * saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram);

        return new FluidNodeState(
            new FluidNodeDefinition("stable", Volume.FromCubicMetres(specificVolume * mass)),
            new FluidNodeInventory(Mass.FromKilograms(mass), Energy.FromJoules(specificEnergy * mass)),
            new FluidThermodynamicState(
                saturation.Pressure,
                saturation.Temperature,
                FluidPhase.SaturatedMixture,
                VaporQuality.FromFraction(quality)));
    }
}
