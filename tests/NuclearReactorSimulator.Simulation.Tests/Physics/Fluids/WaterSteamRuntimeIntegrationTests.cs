using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class WaterSteamRuntimeIntegrationTests
{
    [Fact]
    public void WaterSteamClosure_RemainsPulseSegmentationDeterministic()
    {
        var left = CreateRuntime();
        var right = CreateRuntime();

        left.Resume();
        right.Resume();

        left.Advance(TimeSpan.FromSeconds(2d));

        foreach (var pulse in new[] { 17, 83, 400, 7, 293, 1_200 })
        {
            right.Advance(TimeSpan.FromMilliseconds(pulse));
        }

        var leftSnapshot = left.GetSnapshot();
        var rightSnapshot = right.GetSnapshot();

        Assert.Equal(leftSnapshot, rightSnapshot);
        Assert.Equal(100L, leftSnapshot.Runtime.StepIndex);
        Assert.Equal(FluidPhase.SaturatedMixture, leftSnapshot.State.Phase);
        Assert.True(leftSnapshot.State.VaporQualityFraction > 0d);
    }

    private static SimulationRuntime<PlantState, NoCommand, PlantSnapshot> CreateRuntime()
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
        var node = new FluidNodeState(
            new FluidNodeDefinition("water-steam-runtime", Volume.FromCubicMetres(specificVolume * mass)),
            new FluidNodeInventory(Mass.FromKilograms(mass), Energy.FromJoules(specificEnergy * mass)),
            new FluidThermodynamicState(
                saturation.Pressure,
                saturation.Temperature,
                FluidPhase.SaturatedMixture,
                VaporQuality.FromFraction(quality)));

        return new SimulationRuntime<PlantState, NoCommand, PlantSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            new PlantState(node),
            new PlantKernel());
    }

    private sealed record NoCommand();

    private sealed record PlantState(FluidNodeState Node);

    private sealed record PlantSnapshot(
        double InternalEnergyJoules,
        double PressurePascals,
        double TemperatureKelvins,
        FluidPhase Phase,
        double VaporQualityFraction);

    private sealed class PlantKernel : ISimulationKernel<PlantState, NoCommand, PlantSnapshot>
    {
        private readonly FluidNodeIntegrator _integrator = new(new SimplifiedWaterSteamThermodynamicModel());
        private readonly FluidNodeBalance _balance = new(MassFlowRate.Zero, Power.FromKilowatts(20d));

        public PlantState Step(
            PlantState state,
            IReadOnlyList<QueuedSimulationCommand<NoCommand>> commands,
            SimulationStepContext context)
        {
            _ = commands;
            return new PlantState(_integrator.Step(state.Node, _balance, context.DeltaTime));
        }

        public PlantSnapshot CreateSnapshot(PlantState state)
        {
            return new PlantSnapshot(
                state.Node.InternalEnergy.Joules,
                state.Node.Pressure.Pascals,
                state.Node.Temperature.Kelvins,
                state.Node.Phase,
                state.Node.VaporQuality?.Fraction ?? -1d);
        }
    }
}
