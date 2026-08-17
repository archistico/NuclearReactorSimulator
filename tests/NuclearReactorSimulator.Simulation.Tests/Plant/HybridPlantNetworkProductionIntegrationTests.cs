using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Plant;

public sealed class HybridPlantNetworkProductionIntegrationTests
{
    private static readonly TimeSpan StiffStep = TimeSpan.FromMilliseconds(100d);

    [Fact]
    public void HybridDefinition_RoutesCanonicalNetworkIntegrationThroughDeterministicCorrector()
    {
        var state = CreateTwoNodeState(useHybrid: true);
        var result = new PlantNetworkOrchestrator(new LinearCompressibilityModel()).Step(state, StiffStep);

        Assert.Equal(HydraulicNumericalCouplingMode.DeterministicHybridSemiImplicit, result.HydraulicNumerics.Mode);
        Assert.True(result.HydraulicNumerics.UsedSemiImplicitCorrection);
        Assert.True(result.HydraulicNumerics.Converged, result.HydraulicNumerics.ToString());
        Assert.True(result.HydraulicNumerics.IterationCount >= 2);
        Assert.InRange(Math.Abs(result.Audit.BalanceMassRateResidualKilogramsPerSecond), 0d, 1e-8d);
        Assert.InRange(Math.Abs(result.Audit.MassClosureResidualKilograms), 0d, 1e-6d);
        Assert.InRange(Math.Abs(result.Audit.BalancePowerResidualWatts), 0d, 1e-3d);
        Assert.InRange(Math.Abs(result.Audit.EnergyClosureResidualJoules), 0d, 1e-2d);
    }

    [Fact]
    public void ExplicitDefinition_PreservesHistoricalOnePassNetworkPathAndDiagnostics()
    {
        var state = CreateTwoNodeState(useHybrid: false);
        var result = new PlantNetworkOrchestrator(new LinearCompressibilityModel()).Step(state, StiffStep);

        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, result.HydraulicNumerics.Mode);
        Assert.False(result.HydraulicNumerics.UsedSemiImplicitCorrection);
        Assert.True(result.HydraulicNumerics.Converged);
        Assert.Equal(1, result.HydraulicNumerics.IterationCount);
    }

    private static PlantState CreateTwoNodeState(bool useHybrid)
    {
        var nodeA = new FluidNodeDefinition("a", Volume.FromCubicMetres(1d));
        var nodeB = new FluidNodeDefinition("b", Volume.FromCubicMetres(1d));
        var pipe = new PipeDefinition(
            "link",
            "a",
            "b",
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100d));
        var coupling = useHybrid
            ? HydraulicNumericalCouplingDefinition.CreateDeterministicHybridSemiImplicit(
                0.01d,
                1d,
                96,
                0.10d,
                1e-5d,
                1e-2d)
            : HydraulicNumericalCouplingDefinition.ExplicitCommittedState;
        var definition = new PlantDefinition(
            "hybrid-production-stiff-pair",
            new[] { nodeA, nodeB },
            new[] { pipe },
            Array.Empty<ValveDefinition>(),
            Array.Empty<PumpDefinition>(),
            Array.Empty<ThermalBodyDefinition>(),
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>(),
            coupling);
        var thermodynamics = new LinearCompressibilityModel();

        FluidNodeState State(FluidNodeDefinition node, double mass)
        {
            var inventory = new FluidNodeInventory(
                Mass.FromKilograms(mass),
                Energy.FromMegajoules(mass * 0.5d));
            return new FluidNodeState(
                node,
                inventory,
                thermodynamics.Resolve(
                    node,
                    inventory,
                    new FluidThermodynamicState(
                        Pressure.FromMegapascals(5d),
                        Temperature.FromDegreesCelsius(250d))));
        }

        return new PlantState(
            definition,
            new[] { State(nodeA, 1_001d), State(nodeB, 999d) },
            Array.Empty<ValveState>(),
            Array.Empty<PumpState>(),
            Array.Empty<ThermalBodyState>(),
            Array.Empty<HeatSourceState>());
    }

    private sealed class LinearCompressibilityModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
        {
            _ = definition;
            var pressurePascals = 5_000_000d + ((inventory.Mass.Kilograms - 1_000d) * 100_000d);
            if (pressurePascals <= 0d)
            {
                throw new InvalidOperationException("Synthetic test compressibility left its supported positive-pressure range.");
            }

            return new FluidThermodynamicState(
                Pressure.FromPascals(pressurePascals),
                previousState.Temperature,
                FluidPhase.SubcooledLiquid,
                null);
        }
    }
}
