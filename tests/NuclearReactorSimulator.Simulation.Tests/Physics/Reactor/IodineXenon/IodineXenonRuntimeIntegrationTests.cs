using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.IodineXenon;
using NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Simulation.Physics.Reactor.IodineXenon;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Simulation.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.IodineXenon;

public sealed class IodineXenonRuntimeIntegrationTests
{
    [Fact]
    public void FissionToIodineXenonToReactivityLoop_IsPulseSegmentationDeterministic()
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
        Assert.True(left.State.IodineInventory > 0d);
        Assert.True(left.State.XenonInventory > 0d);
        Assert.True(left.State.XenonReactivityPcm < 0d);
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

        return new SimulationRuntime<PlantState, NoCommand, PlantSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            new PlantState(
                PointKineticsState.CreateCriticalEquilibrium(kineticsParameters, NeutronPopulation.Reference),
                IodineXenonState.Empty),
            new PlantKernel(kineticsParameters));
    }

    private sealed record NoCommand;

    private sealed record PlantState(PointKineticsState Kinetics, IodineXenonState Poison);

    private sealed record PlantSnapshot(
        double NeutronPopulation,
        double FissionThermalPowerMegawatts,
        double IodineInventory,
        double XenonInventory,
        double XenonReactivityPcm);

    private sealed class PlantKernel : ISimulationKernel<PlantState, NoCommand, PlantSnapshot>
    {
        private readonly PointKineticsSolver _kinetics;
        private readonly FissionPowerSolver _fission;
        private readonly IodineXenonSolver _poison;

        public PlantKernel(PointKineticsParameters kineticsParameters)
        {
            _kinetics = new PointKineticsSolver(kineticsParameters);
            _fission = new FissionPowerSolver(new FissionPowerDefinition(
                "core-fission",
                new FissionPowerCalibration(NeutronPopulation.Reference, Power.FromMegawatts(1_000d)),
                [new FissionHeatDestinationDefinition("fuel", HeatDepositionFraction.Full)]));
            _poison = new IodineXenonSolver(new IodineXenonDefinition(
                "core",
                Power.FromMegawatts(1_000d),
                PoisonProductionRate.FromRelativePerSecond(0.02d),
                PoisonProductionRate.FromRelativePerSecond(0.01d),
                DecayConstant.FromPerSecond(0.5d),
                DecayConstant.FromPerSecond(0.05d),
                XenonBurnupCoefficient.FromPerSecondPerRelativeNeutronPopulation(0.1d),
                XenonReactivityCoefficient.FromPcmPerRelativeInventory(-500d)));
        }

        public PlantState Step(
            PlantState state,
            IReadOnlyList<QueuedSimulationCommand<NoCommand>> commands,
            SimulationStepContext context)
        {
            _ = commands;

            var committedFission = _fission.Solve(state.Kinetics.NeutronPopulation);
            var committedPoison = _poison.CreateSnapshot(
                state.Poison,
                committedFission.TotalFissionThermalPower,
                state.Kinetics.NeutronPopulation);
            var kinetics = _kinetics.Step(
                state.Kinetics,
                committedPoison.XenonReactivity,
                context.DeltaTime);
            var updatedFission = _fission.Solve(kinetics.NeutronPopulation);
            var poison = _poison.Step(
                state.Poison,
                updatedFission.TotalFissionThermalPower,
                kinetics.NeutronPopulation,
                context.DeltaTime);

            return state with { Kinetics = kinetics, Poison = poison.State };
        }

        public PlantSnapshot CreateSnapshot(PlantState state)
        {
            var fission = _fission.Solve(state.Kinetics.NeutronPopulation);
            var poison = _poison.CreateSnapshot(
                state.Poison,
                fission.TotalFissionThermalPower,
                state.Kinetics.NeutronPopulation);

            return new PlantSnapshot(
                state.Kinetics.NeutronPopulation.Relative,
                fission.TotalFissionThermalPower.Megawatts,
                state.Poison.Iodine.Relative,
                state.Poison.Xenon.Relative,
                poison.XenonReactivity.Pcm);
        }
    }
}
