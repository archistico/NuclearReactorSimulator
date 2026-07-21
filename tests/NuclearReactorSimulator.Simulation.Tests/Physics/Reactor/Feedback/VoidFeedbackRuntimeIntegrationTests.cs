using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Domain.Physics.Reactor.Feedback;
using NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Feedback;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Simulation.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.Feedback;

public sealed class VoidFeedbackRuntimeIntegrationTests
{
    [Fact]
    public void WaterSteamVoidFeedback_ClosesThermohydraulicToNeutronicPathDeterministically()
    {
        var singlePulse = CreateRuntime(out var initialVoidPercent);
        var irregularPulses = CreateRuntime(out var secondInitialVoidPercent);
        Assert.Equal(initialVoidPercent, secondInitialVoidPercent, 12);

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
        Assert.Equal(FluidPhase.SaturatedMixture, left.State.Phase);
        Assert.NotEqual(initialVoidPercent, left.State.VoidPercent);
        Assert.NotEqual(0d, left.State.VoidFeedbackPcm);
        Assert.Equal(
            Math.Sign(left.State.VoidPercent - initialVoidPercent),
            Math.Sign(left.State.VoidFeedbackPcm));
        Assert.NotEqual(1d, left.State.NeutronPopulation);
        Assert.NotEqual(2d, left.State.FissionThermalPowerMegawatts);
    }

    private static SimulationRuntime<PlantState, NoCommand, PlantSnapshot> CreateRuntime(
        out double initialVoidPercent)
    {
        var thermodynamicModel = new SimplifiedWaterSteamThermodynamicModel();
        var voidFractionSolver = new WaterSteamVoidFractionSolver(thermodynamicModel);
        var saturation = thermodynamicModel.GetSaturationProperties(Temperature.FromDegreesCelsius(150d));
        const double quality = 0.001d;
        const double mass = 500d;
        var specificVolume =
            ((1d - quality) * saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram)
            + (quality * saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram);
        var specificEnergy =
            ((1d - quality) * saturation.SaturatedLiquidInternalEnergy.JoulesPerKilogram)
            + (quality * saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram);
        var node = new FluidNodeState(
            new FluidNodeDefinition("core-coolant", Volume.FromCubicMetres(specificVolume * mass)),
            new FluidNodeInventory(Mass.FromKilograms(mass), Energy.FromJoules(specificEnergy * mass)),
            new FluidThermodynamicState(
                saturation.Pressure,
                saturation.Temperature,
                FluidPhase.SaturatedMixture,
                VaporQuality.FromFraction(quality)));
        var initialVoid = voidFractionSolver.Resolve(node.Thermodynamics);
        initialVoidPercent = initialVoid.Percent;

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

        return new SimulationRuntime<PlantState, NoCommand, PlantSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            new PlantState(
                PointKineticsState.CreateCriticalEquilibrium(kineticsParameters, NeutronPopulation.Reference),
                node),
            new PlantKernel(kineticsParameters, thermodynamicModel, initialVoid));
    }

    private sealed record NoCommand;

    private sealed record PlantState(PointKineticsState Kinetics, FluidNodeState Coolant);

    private sealed record PlantSnapshot(
        double NeutronPopulation,
        double FissionThermalPowerMegawatts,
        FluidPhase Phase,
        double VaporQualityFraction,
        double VoidPercent,
        double VoidFeedbackPcm);

    private sealed class PlantKernel : ISimulationKernel<PlantState, NoCommand, PlantSnapshot>
    {
        private readonly PointKineticsSolver _kineticsSolver;
        private readonly FissionPowerSolver _fissionPowerSolver;
        private readonly FluidNodeIntegrator _fluidNodeIntegrator;
        private readonly WaterSteamVoidFractionSolver _voidFractionSolver;
        private readonly VoidFeedbackSolver _voidFeedbackSolver = new();
        private readonly ReactivityModel _reactivityModel = new();
        private readonly VoidReactivityFeedbackDefinition _voidFeedback;

        public PlantKernel(
            PointKineticsParameters kineticsParameters,
            SimplifiedWaterSteamThermodynamicModel thermodynamicModel,
            VoidFraction initialVoid)
        {
            _kineticsSolver = new PointKineticsSolver(kineticsParameters);
            _fissionPowerSolver = new FissionPowerSolver(new FissionPowerDefinition(
                "core-fission",
                new FissionPowerCalibration(NeutronPopulation.Reference, Power.FromMegawatts(2d)),
                [new FissionHeatDestinationDefinition("coolant", HeatDepositionFraction.FromFraction(1d))]));
            _fluidNodeIntegrator = new FluidNodeIntegrator(thermodynamicModel);
            _voidFractionSolver = new WaterSteamVoidFractionSolver(thermodynamicModel);
            _voidFeedback = new VoidReactivityFeedbackDefinition(
                "void/core",
                initialVoid,
                VoidReactivityCoefficient.FromPcmPerPercentVoid(0.5d));
        }

        public PlantState Step(
            PlantState state,
            IReadOnlyList<QueuedSimulationCommand<NoCommand>> commands,
            SimulationStepContext context)
        {
            _ = commands;

            var voidFraction = _voidFractionSolver.Resolve(state.Coolant.Thermodynamics);
            var voidFeedback = _voidFeedbackSolver.Evaluate(
                new VoidFeedbackInput(_voidFeedback, voidFraction));
            var totalReactivity = _reactivityModel.Evaluate([voidFeedback.ToContribution()]).Total;
            var kinetics = _kineticsSolver.Step(state.Kinetics, totalReactivity, context.DeltaTime);
            var fission = _fissionPowerSolver.Solve(kinetics.NeutronPopulation);
            var coolant = _fluidNodeIntegrator.Step(
                state.Coolant,
                fission.GetDeposition("coolant").ToFluidNodeBalance(),
                context.DeltaTime);

            return state with { Kinetics = kinetics, Coolant = coolant };
        }

        public PlantSnapshot CreateSnapshot(PlantState state)
        {
            var voidFraction = _voidFractionSolver.Resolve(state.Coolant.Thermodynamics);
            var feedback = _voidFeedbackSolver.Evaluate(
                new VoidFeedbackInput(_voidFeedback, voidFraction));
            var fission = _fissionPowerSolver.Solve(state.Kinetics.NeutronPopulation);

            return new PlantSnapshot(
                state.Kinetics.NeutronPopulation.Relative,
                fission.TotalFissionThermalPower.Megawatts,
                state.Coolant.Phase,
                state.Coolant.VaporQuality?.Fraction ?? -1d,
                voidFraction.Percent,
                feedback.Reactivity.Pcm);
        }
    }
}
