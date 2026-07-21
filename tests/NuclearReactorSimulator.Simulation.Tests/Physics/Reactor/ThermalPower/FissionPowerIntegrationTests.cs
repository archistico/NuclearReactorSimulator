using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.ThermalPower;

public sealed class FissionPowerIntegrationTests
{
    [Fact]
    public void DistributedFissionHeat_ChangesOnlyEnergyAndClosesPlantEnergyInput()
    {
        var solver = new FissionPowerSolver(Definition());
        var snapshot = solver.Solve(NeutronPopulation.Reference);
        var fuelIntegrator = new ThermalBodyIntegrator();
        var coolantIntegrator = new FluidNodeIntegrator(new PreserveThermodynamicsModel());
        var fuel = ThermalBodyState.FromTemperature(
            new ThermalBodyDefinition("fuel", HeatCapacity.FromMegajoulesPerKelvin(10d)),
            Temperature.FromDegreesCelsius(500d));
        var coolant = Coolant();
        var initialFuelEnergy = fuel.StoredThermalEnergy;
        var initialCoolantEnergy = coolant.InternalEnergy;
        var duration = TimeSpan.FromSeconds(2d);

        fuel = fuelIntegrator.Step(
            fuel,
            snapshot.GetDeposition("fuel").ToThermalEnergyBalance(),
            duration);
        coolant = coolantIntegrator.Step(
            coolant,
            snapshot.GetDeposition("coolant").ToFluidNodeBalance(),
            duration);

        var depositedEnergy = (fuel.StoredThermalEnergy - initialFuelEnergy)
            + (coolant.InternalEnergy - initialCoolantEnergy);

        Assert.Equal(snapshot.TotalFissionThermalPower.Over(duration).Joules, depositedEnergy.Joules, 5);
        Assert.Equal(Mass.FromKilograms(10_000d), coolant.Mass);
    }

    private static FissionPowerDefinition Definition()
        => new(
            "core-fission",
            new FissionPowerCalibration(NeutronPopulation.Reference, Power.FromMegawatts(1_000d)),
            [
                new FissionHeatDestinationDefinition("fuel", HeatDepositionFraction.FromFraction(0.8d)),
                new FissionHeatDestinationDefinition("coolant", HeatDepositionFraction.FromFraction(0.2d)),
            ]);

    private static FluidNodeState Coolant()
        => new(
            new FluidNodeDefinition("coolant", Volume.FromCubicMetres(12d)),
            new FluidNodeInventory(Mass.FromKilograms(10_000d), Energy.FromMegajoules(10_000d)),
            new FluidThermodynamicState(
                Pressure.FromMegapascals(7d),
                Temperature.FromDegreesCelsius(280d)));

    private sealed class PreserveThermodynamicsModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
            => previousState;
    }
}
