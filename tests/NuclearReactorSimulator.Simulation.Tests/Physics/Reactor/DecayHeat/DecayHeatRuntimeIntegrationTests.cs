using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.DecayHeat;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Physics.Reactor.DecayHeat;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.DecayHeat;

public sealed class DecayHeatRuntimeIntegrationTests
{
    [Fact]
    public void PostShutdownDecayHeat_IsIndependentFromExternalPulseSegmentation()
    {
        var singlePulse = CreateRuntime();
        var irregularPulses = CreateRuntime();

        singlePulse.EnqueueCommand(new SetFissionPowerCommand(Power.Zero));
        irregularPulses.EnqueueCommand(new SetFissionPowerCommand(Power.Zero));
        singlePulse.Resume();
        irregularPulses.Resume();

        singlePulse.Advance(TimeSpan.FromSeconds(2d));
        foreach (var milliseconds in new[] { 17, 83, 400, 7, 293, 1_200 })
        {
            irregularPulses.Advance(TimeSpan.FromMilliseconds(milliseconds));
        }

        var left = singlePulse.GetSnapshot();
        var right = irregularPulses.GetSnapshot();

        Assert.Equal(left, right);
        Assert.Equal(100L, left.Runtime.StepIndex);
        Assert.Equal(0d, left.State.FissionThermalPowerMegawatts, 12);
        Assert.True(left.State.DecayHeatPowerMegawatts > 0d);
        Assert.True(left.State.DecayHeatPowerMegawatts < 60d);
        Assert.True(left.State.FuelStoredEnergyJoules > InitialFuel().StoredThermalEnergy.Joules);
        Assert.True(left.State.StoredDecayEnergyJoules < InitialDecayState().TotalStoredDecayEnergy.Joules);
    }

    private static SimulationRuntime<PlantState, SetFissionPowerCommand, PlantSnapshot> CreateRuntime()
    {
        return new SimulationRuntime<PlantState, SetFissionPowerCommand, PlantSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            new PlantState(
                InitialDecayState(),
                Power.FromMegawatts(1_000d),
                InitialFuel()),
            new PlantKernel(Definition()));
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
            [new DecayHeatDestinationDefinition("fuel", HeatDepositionFraction.Full)]);

    private static DecayHeatState InitialDecayState()
        => DecayHeatState.CreateEquilibrium(Definition(), Power.FromMegawatts(1_000d));

    private static ThermalBodyState InitialFuel()
        => ThermalBodyState.FromTemperature(
            new ThermalBodyDefinition("fuel", HeatCapacity.FromMegajoulesPerKelvin(50d)),
            Temperature.FromDegreesCelsius(700d));

    private sealed record SetFissionPowerCommand(Power FissionThermalPower);

    private sealed record PlantState(
        DecayHeatState DecayHeat,
        Power FissionThermalPower,
        ThermalBodyState Fuel);

    private sealed record PlantSnapshot(
        double FissionThermalPowerMegawatts,
        double DecayHeatPowerMegawatts,
        double StoredDecayEnergyJoules,
        double FuelStoredEnergyJoules);

    private sealed class PlantKernel : ISimulationKernel<PlantState, SetFissionPowerCommand, PlantSnapshot>
    {
        private readonly DecayHeatSolver _decayHeatSolver;
        private readonly ThermalBodyIntegrator _thermalBodyIntegrator = new();

        public PlantKernel(DecayHeatDefinition definition)
        {
            _decayHeatSolver = new DecayHeatSolver(definition);
        }

        public PlantState Step(
            PlantState state,
            IReadOnlyList<QueuedSimulationCommand<SetFissionPowerCommand>> commands,
            SimulationStepContext context)
        {
            foreach (var queued in commands)
            {
                state = state with { FissionThermalPower = queued.Command.FissionThermalPower };
            }

            var decay = _decayHeatSolver.Step(state.DecayHeat, state.FissionThermalPower, context.DeltaTime);
            var fuel = _thermalBodyIntegrator.Step(
                state.Fuel,
                decay.GetAverageDeposition("fuel").ToThermalEnergyBalance(),
                context.DeltaTime);

            return state with
            {
                DecayHeat = decay.State,
                Fuel = fuel,
            };
        }

        public PlantSnapshot CreateSnapshot(PlantState state)
        {
            var decay = _decayHeatSolver.CreateSnapshot(state.DecayHeat);
            return new PlantSnapshot(
                state.FissionThermalPower.Megawatts,
                decay.TotalInstantaneousDecayHeatPower.Megawatts,
                decay.TotalStoredDecayEnergy.Joules,
                state.Fuel.StoredThermalEnergy.Joules);
        }
    }
}
