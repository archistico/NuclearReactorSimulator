using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.DecayHeat;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.DecayHeat;
using NuclearReactorSimulator.Simulation.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.DecayHeat;

public sealed class DecayHeatIntegrationTests
{
    [Fact]
    public void M24FissionPowerOutput_DrivesDecayInventoryProductionExplicitly()
    {
        var fission = new FissionPowerSolver(new FissionPowerDefinition(
            "core-fission",
            new FissionPowerCalibration(NeutronPopulation.Reference, Power.FromMegawatts(1_000d)),
            [new FissionHeatDestinationDefinition("fuel", HeatDepositionFraction.Full)]))
            .Solve(NeutronPopulation.Reference);
        var definition = Definition();
        var solver = new DecayHeatSolver(definition);
        var initial = DecayHeatState.CreateEmpty(definition);

        var result = solver.Step(initial, fission.TotalFissionThermalPower, TimeSpan.FromSeconds(1d));

        Assert.Equal(60d, result.PrecursorProductionPower.Megawatts, 9);
        Assert.Equal(60d, result.ProducedDecayEnergy.Megajoules, 9);
        Assert.True(result.State.TotalStoredDecayEnergy > Energy.Zero);
    }

    [Fact]
    public void ShutdownDecayHeat_TransfersLatentEnergyIntoFuelAndCoolantWithoutCreatingMass()
    {
        var definition = Definition();
        var solver = new DecayHeatSolver(definition);
        var decay = DecayHeatState.CreateEquilibrium(definition, Power.FromMegawatts(1_000d));
        var fuel = InitialFuel();
        var coolant = InitialCoolant();
        var fuelIntegrator = new ThermalBodyIntegrator();
        var coolantIntegrator = new FluidNodeIntegrator(new PreserveThermodynamicsModel());
        var duration = TimeSpan.FromSeconds(2d);

        var result = solver.Step(decay, Power.Zero, duration);
        var nextFuel = fuelIntegrator.Step(fuel, result.GetAverageDeposition("fuel").ToThermalEnergyBalance(), duration);
        var nextCoolant = coolantIntegrator.Step(coolant, result.GetAverageDeposition("coolant").ToFluidNodeBalance(), duration);

        var deposited = (nextFuel.StoredThermalEnergy - fuel.StoredThermalEnergy)
            + (nextCoolant.InternalEnergy - coolant.InternalEnergy);

        Assert.Equal(result.EmittedDecayEnergy.Joules, deposited.Joules, 3);
        Assert.Equal(decay.TotalStoredDecayEnergy.Joules, result.State.TotalStoredDecayEnergy.Joules + deposited.Joules, 3);
        Assert.Equal(coolant.Mass, nextCoolant.Mass);
    }

    private static DecayHeatDefinition Definition()
        => new(
            "core-decay",
            [
                new DecayHeatGroupDefinition(
                    "fast",
                    DecayHeatGenerationFraction.FromFraction(0.04d),
                    DecayConstant.FromHalfLife(TimeSpan.FromSeconds(5d))),
                new DecayHeatGroupDefinition(
                    "slow",
                    DecayHeatGenerationFraction.FromFraction(0.02d),
                    DecayConstant.FromHalfLife(TimeSpan.FromSeconds(50d))),
            ],
            [
                new DecayHeatDestinationDefinition("fuel", HeatDepositionFraction.FromFraction(0.8d)),
                new DecayHeatDestinationDefinition("coolant", HeatDepositionFraction.FromFraction(0.2d)),
            ]);

    private static ThermalBodyState InitialFuel()
        => ThermalBodyState.FromTemperature(
            new ThermalBodyDefinition("fuel", HeatCapacity.FromMegajoulesPerKelvin(50d)),
            Temperature.FromDegreesCelsius(700d));

    private static FluidNodeState InitialCoolant()
        => new(
            new FluidNodeDefinition("coolant", Volume.FromCubicMetres(20d)),
            new FluidNodeInventory(
                Mass.FromKilograms(15_000d),
                Energy.FromMegajoules(25_000d)),
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
