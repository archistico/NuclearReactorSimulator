using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Simulation.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.ThermalPower;

public sealed class FissionPowerRuntimeIntegrationTests
{
    [Fact]
    public void NeutronDrivenFissionHeat_IsIndependentFromExternalPulseSegmentation()
    {
        var singlePulse = CreateRuntime();
        var irregularPulses = CreateRuntime();

        singlePulse.EnqueueCommand(new SetReactivityCommand(Reactivity.FromPcm(100d)));
        irregularPulses.EnqueueCommand(new SetReactivityCommand(Reactivity.FromPcm(100d)));
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
        Assert.True(left.State.NeutronPopulation > 1d);
        Assert.True(left.State.FissionThermalPowerMegawatts > 1_000d);
        Assert.True(left.State.FuelStoredEnergyJoules > InitialFuel().StoredThermalEnergy.Joules);
        Assert.True(left.State.CoolantInternalEnergyJoules > InitialCoolant().InternalEnergy.Joules);
    }

    private static SimulationRuntime<PlantState, SetReactivityCommand, PlantSnapshot> CreateRuntime()
    {
        var kineticsParameters = KineticsParameters();
        return new SimulationRuntime<PlantState, SetReactivityCommand, PlantSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            new PlantState(
                PointKineticsState.CreateCriticalEquilibrium(kineticsParameters, NeutronPopulation.Reference),
                Reactivity.Zero,
                InitialFuel(),
                InitialCoolant()),
            new PlantKernel(kineticsParameters, FissionDefinition()));
    }

    private static FissionPowerDefinition FissionDefinition()
        => new(
            "core-fission",
            new FissionPowerCalibration(NeutronPopulation.Reference, Power.FromMegawatts(1_000d)),
            [
                new FissionHeatDestinationDefinition("fuel", HeatDepositionFraction.FromFraction(0.8d)),
                new FissionHeatDestinationDefinition("coolant", HeatDepositionFraction.FromFraction(0.2d)),
            ]);

    private static PointKineticsParameters KineticsParameters()
        => new(
            TimeSpan.FromMilliseconds(5d),
            [
                new DelayedNeutronGroupDefinition(
                    "slow",
                    DelayedNeutronFraction.FromFraction(0.004d),
                    DecayConstant.FromPerSecond(0.08d)),
                new DelayedNeutronGroupDefinition(
                    "fast",
                    DelayedNeutronFraction.FromFraction(0.0025d),
                    DecayConstant.FromPerSecond(0.8d)),
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

    private sealed record SetReactivityCommand(Reactivity Reactivity);

    private sealed record PlantState(
        PointKineticsState Kinetics,
        Reactivity Reactivity,
        ThermalBodyState Fuel,
        FluidNodeState Coolant);

    private sealed record PlantSnapshot(
        double NeutronPopulation,
        double FissionThermalPowerMegawatts,
        double FuelStoredEnergyJoules,
        double CoolantInternalEnergyJoules);

    private sealed class PlantKernel : ISimulationKernel<PlantState, SetReactivityCommand, PlantSnapshot>
    {
        private readonly PointKineticsSolver _kineticsSolver;
        private readonly FissionPowerSolver _fissionPowerSolver;
        private readonly ThermalBodyIntegrator _thermalBodyIntegrator = new();
        private readonly FluidNodeIntegrator _fluidNodeIntegrator = new(new PreserveThermodynamicsModel());

        public PlantKernel(
            PointKineticsParameters kineticsParameters,
            FissionPowerDefinition fissionPowerDefinition)
        {
            _kineticsSolver = new PointKineticsSolver(kineticsParameters);
            _fissionPowerSolver = new FissionPowerSolver(fissionPowerDefinition);
        }

        public PlantState Step(
            PlantState state,
            IReadOnlyList<QueuedSimulationCommand<SetReactivityCommand>> commands,
            SimulationStepContext context)
        {
            foreach (var queued in commands)
            {
                state = state with { Reactivity = queued.Command.Reactivity };
            }

            var kinetics = _kineticsSolver.Step(state.Kinetics, state.Reactivity, context.DeltaTime);
            var fission = _fissionPowerSolver.Solve(kinetics.NeutronPopulation);
            var fuel = _thermalBodyIntegrator.Step(
                state.Fuel,
                fission.GetDeposition("fuel").ToThermalEnergyBalance(),
                context.DeltaTime);
            var coolant = _fluidNodeIntegrator.Step(
                state.Coolant,
                fission.GetDeposition("coolant").ToFluidNodeBalance(),
                context.DeltaTime);

            return state with
            {
                Kinetics = kinetics,
                Fuel = fuel,
                Coolant = coolant,
            };
        }

        public PlantSnapshot CreateSnapshot(PlantState state)
        {
            var fission = _fissionPowerSolver.Solve(state.Kinetics.NeutronPopulation);
            return new PlantSnapshot(
                state.Kinetics.NeutronPopulation.Relative,
                fission.TotalFissionThermalPower.Megawatts,
                state.Fuel.StoredThermalEnergy.Joules,
                state.Coolant.InternalEnergy.Joules);
        }
    }

    private sealed class PreserveThermodynamicsModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
            => previousState;
    }
}
