using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Thermal;

public sealed class ThermalBodyIntegratorTests
{
    [Fact]
    public void PositiveHeatRate_AddsEnergyAndRaisesTemperature()
    {
        var state = CreateState(100d, 20d);

        var next = new ThermalBodyIntegrator().Step(
            state,
            new ThermalEnergyBalance(Power.FromMegawatts(2d)),
            TimeSpan.FromSeconds(10d));

        Assert.Equal(state.StoredThermalEnergy.Megajoules + 20d, next.StoredThermalEnergy.Megajoules, 9);
        Assert.Equal(state.Temperature.Kelvins + 0.2d, next.Temperature.Kelvins, 12);
    }

    [Fact]
    public void NegativeHeatRate_RemovesEnergyAndLowersTemperature()
    {
        var state = CreateState(50d, 100d);

        var next = new ThermalBodyIntegrator().Step(
            state,
            new ThermalEnergyBalance(Power.FromMegawatts(-1d)),
            TimeSpan.FromSeconds(5d));

        Assert.Equal(state.StoredThermalEnergy.Megajoules - 5d, next.StoredThermalEnergy.Megajoules, 9);
        Assert.Equal(state.Temperature.Kelvins - 0.1d, next.Temperature.Kelvins, 12);
    }

    [Fact]
    public void ZeroBalance_PreservesEntireThermalState()
    {
        var state = CreateState(50d, 200d);

        var next = new ThermalBodyIntegrator().Step(
            state,
            ThermalEnergyBalance.Zero,
            TimeSpan.FromSeconds(1d));

        Assert.Equal(state, next);
    }

    [Fact]
    public void IntegrationRejectsNonPositiveDuration()
    {
        var state = CreateState(50d, 200d);
        var integrator = new ThermalBodyIntegrator();

        Assert.Throws<ArgumentOutOfRangeException>(() => integrator.Step(
            state,
            ThermalEnergyBalance.Zero,
            TimeSpan.Zero));
    }

    [Fact]
    public void CoolingBelowAbsoluteZeroEnergy_FailsFast()
    {
        var definition = new ThermalBodyDefinition(
            "wall",
            HeatCapacity.FromJoulesPerKelvin(1d));
        var state = new ThermalBodyState(definition, Energy.FromJoules(1d));

        Assert.Throws<ThermalBodyEnergyDepletionException>(() => new ThermalBodyIntegrator().Step(
            state,
            new ThermalEnergyBalance(Power.FromWatts(-2d)),
            TimeSpan.FromSeconds(1d)));
    }

    private static ThermalBodyState CreateState(double megajoulesPerKelvin, double celsius)
    {
        var definition = new ThermalBodyDefinition(
            "wall",
            HeatCapacity.FromMegajoulesPerKelvin(megajoulesPerKelvin));
        return ThermalBodyState.FromTemperature(
            definition,
            Temperature.FromDegreesCelsius(celsius));
    }
}
