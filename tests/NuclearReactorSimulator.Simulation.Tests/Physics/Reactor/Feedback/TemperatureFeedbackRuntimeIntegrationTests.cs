using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Domain.Physics.Reactor.Feedback;
using NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Physics.Reactor;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Feedback;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Simulation.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.Feedback;

public sealed class TemperatureFeedbackRuntimeIntegrationTests
{
    [Fact]
    public void FuelHeating_ClosesNegativeTemperatureFeedbackLoopDeterministically()
    {
        var singlePulse = CreateRuntime();
        var irregularPulses = CreateRuntime();

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
        Assert.True(left.State.FuelTemperatureCelsius > 700d);
        Assert.True(left.State.TemperatureFeedbackPcm < 0d);
        Assert.True(left.State.NeutronPopulation < 1d);
        Assert.True(left.State.FissionThermalPowerMegawatts < 1_000d);
    }

    private static SimulationRuntime<PlantState, NoCommand, PlantSnapshot> CreateRuntime()
    {
        var kineticsParameters = new PointKineticsParameters(
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

        var fuel = ThermalBodyState.FromTemperature(
            new ThermalBodyDefinition("fuel", HeatCapacity.FromMegajoulesPerKelvin(1_000d)),
            Temperature.FromDegreesCelsius(700d));

        return new SimulationRuntime<PlantState, NoCommand, PlantSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            new PlantState(
                PointKineticsState.CreateCriticalEquilibrium(kineticsParameters, NeutronPopulation.Reference),
                fuel),
            new PlantKernel(kineticsParameters));
    }

    private sealed record NoCommand;

    private sealed record PlantState(PointKineticsState Kinetics, ThermalBodyState Fuel);

    private sealed record PlantSnapshot(
        double NeutronPopulation,
        double FissionThermalPowerMegawatts,
        double FuelTemperatureCelsius,
        double TemperatureFeedbackPcm);

    private sealed class PlantKernel : ISimulationKernel<PlantState, NoCommand, PlantSnapshot>
    {
        private readonly PointKineticsSolver _kineticsSolver;
        private readonly FissionPowerSolver _fissionPowerSolver;
        private readonly TemperatureFeedbackSolver _temperatureFeedbackSolver = new();
        private readonly ReactivityModel _reactivityModel = new();
        private readonly ThermalBodyIntegrator _thermalBodyIntegrator = new();
        private readonly TemperatureReactivityFeedbackDefinition _fuelFeedback = new(
            "fuel-temperature/core",
            ReactivityContributionKind.FuelTemperature,
            Temperature.FromDegreesCelsius(700d),
            TemperatureReactivityCoefficient.FromPcmPerKelvin(-5d));

        public PlantKernel(PointKineticsParameters kineticsParameters)
        {
            _kineticsSolver = new PointKineticsSolver(kineticsParameters);
            _fissionPowerSolver = new FissionPowerSolver(new FissionPowerDefinition(
                "core-fission",
                new FissionPowerCalibration(NeutronPopulation.Reference, Power.FromMegawatts(1_000d)),
                [new FissionHeatDestinationDefinition("fuel", HeatDepositionFraction.FromFraction(1d))]));
        }

        public PlantState Step(
            PlantState state,
            IReadOnlyList<QueuedSimulationCommand<NoCommand>> commands,
            SimulationStepContext context)
        {
            var temperatureFeedback = _temperatureFeedbackSolver.Evaluate(
                new TemperatureFeedbackInput(_fuelFeedback, state.Fuel.Temperature));
            var totalReactivity = _reactivityModel.Evaluate(
                [temperatureFeedback.ToContribution()]).Total;
            var kinetics = _kineticsSolver.Step(state.Kinetics, totalReactivity, context.DeltaTime);
            var fission = _fissionPowerSolver.Solve(kinetics.NeutronPopulation);
            var fuel = _thermalBodyIntegrator.Step(
                state.Fuel,
                fission.GetDeposition("fuel").ToThermalEnergyBalance(),
                context.DeltaTime);

            return state with { Kinetics = kinetics, Fuel = fuel };
        }

        public PlantSnapshot CreateSnapshot(PlantState state)
        {
            var feedback = _temperatureFeedbackSolver.Evaluate(
                new TemperatureFeedbackInput(_fuelFeedback, state.Fuel.Temperature));
            var fission = _fissionPowerSolver.Solve(state.Kinetics.NeutronPopulation);

            return new PlantSnapshot(
                state.Kinetics.NeutronPopulation.Relative,
                fission.TotalFissionThermalPower.Megawatts,
                state.Fuel.Temperature.DegreesCelsius,
                feedback.Reactivity.Pcm);
        }
    }
}
