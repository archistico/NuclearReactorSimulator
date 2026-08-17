using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Plant;

public sealed class SemiImplicitHydraulicPrototypeSolverTests
{
    private static readonly TimeSpan StiffStep = TimeSpan.FromMilliseconds(100d);

    [Fact]
    public void Options_RejectInvalidIterationRelaxationAndToleranceValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SemiImplicitHydraulicPrototypeOptions(1, 0.1d, 1e-6d, 1e-3d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SemiImplicitHydraulicPrototypeOptions(8, 0d, 1e-6d, 1e-3d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SemiImplicitHydraulicPrototypeOptions(8, 1.1d, 1e-6d, 1e-3d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SemiImplicitHydraulicPrototypeOptions(8, 0.1d, 0d, 1e-3d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SemiImplicitHydraulicPrototypeOptions(8, 0.1d, 1e-6d, 0d));
    }

    [Fact]
    public void Evaluate_PreservesHydraulicMassAndEnergyOwnershipClosure()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var result = new SemiImplicitHydraulicPrototypeSolver(new LinearCompressibilityModel()).Evaluate(state);

        Assert.Single(result.PipeMassFlowRates);
        Assert.True(result.GetPipeMassFlowRate("link") > MassFlowRate.Zero);
        Assert.InRange(result.MassRateClosureResidualKilogramsPerSecond, 0d, 1e-12d);
        Assert.InRange(result.HydraulicEnergyOwnershipResidualWatts, 0d, 1e-6d);
    }

    [Fact]
    public void SemiImplicitStep_ConvergesAndSuppressesExplicitPressureOvershootOnStiffPair()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var solver = new SemiImplicitHydraulicPrototypeSolver(new LinearCompressibilityModel());
        var empty = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal);

        var explicitStep = solver.StepExplicit(state, StiffStep, empty);
        var semiImplicitStep = solver.StepSemiImplicit(state, StiffStep, empty);

        var initialDifference = PressureDifferenceMagnitude(state);
        var explicitDifference = PressureDifferenceMagnitude(explicitStep.CandidateState);
        var semiImplicitDifference = PressureDifferenceMagnitude(semiImplicitStep.CandidateState);

        Assert.True(semiImplicitStep.Converged, semiImplicitStep.ToString());
        Assert.InRange(semiImplicitStep.IterationCount, 2, SemiImplicitHydraulicPrototypeOptions.H3AuditDefault.MaximumIterations);
        Assert.True(explicitDifference > initialDifference);
        Assert.True(semiImplicitDifference < explicitDifference);
        Assert.True(semiImplicitDifference < initialDifference);
        Assert.True(semiImplicitStep.HydraulicEvaluation.GetPipeMassFlowRate("link") > MassFlowRate.Zero);
    }

    [Fact]
    public void SemiImplicitStep_PreservesReverseFlowSemanticsOffDesign()
    {
        var state = CreateTwoNodeState(999d, 1_001d);
        var solver = new SemiImplicitHydraulicPrototypeSolver(new LinearCompressibilityModel());

        var result = solver.StepSemiImplicit(
            state,
            StiffStep,
            new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal));

        Assert.True(result.Converged, result.ToString());
        Assert.True(result.HydraulicEvaluation.GetPipeMassFlowRate("link") < MassFlowRate.Zero);
        Assert.True(result.CandidateState.GetFluidNode("a").Mass > state.GetFluidNode("a").Mass);
        Assert.True(result.CandidateState.GetFluidNode("b").Mass < state.GetFluidNode("b").Mass);
    }

    [Fact]
    public void SemiImplicitStep_IsExactlyDeterministicForTheSameInputs()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var solver = new SemiImplicitHydraulicPrototypeSolver(new LinearCompressibilityModel());
        var frozen = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal)
        {
            ["a"] = new FluidNodeBalance(MassFlowRate.FromKilogramsPerSecond(0.1d), Power.FromKilowatts(5d)),
            ["b"] = new FluidNodeBalance(MassFlowRate.FromKilogramsPerSecond(-0.1d), Power.FromKilowatts(-5d)),
        };

        var left = solver.StepSemiImplicit(state, TimeSpan.FromMilliseconds(20d), frozen);
        var right = solver.StepSemiImplicit(state, TimeSpan.FromMilliseconds(20d), frozen);

        Assert.Equal(left.IterationCount, right.IterationCount);
        Assert.Equal(left.Converged, right.Converged);
        Assert.Equal(left.MaximumRelativePressureResidual, right.MaximumRelativePressureResidual);
        Assert.Equal(left.MaximumAbsoluteFlowResidualKilogramsPerSecond, right.MaximumAbsoluteFlowResidualKilogramsPerSecond);
        Assert.Equal(
            left.CandidateState.FluidNodes.Select(static node => (node.Id, node.Mass.Kilograms, node.InternalEnergy.Joules, node.Pressure.Pascals)),
            right.CandidateState.FluidNodes.Select(static node => (node.Id, node.Mass.Kilograms, node.InternalEnergy.Joules, node.Pressure.Pascals)));
    }

    [Fact]
    public void FrozenBalanceForUnknownNode_IsRejected()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var solver = new SemiImplicitHydraulicPrototypeSolver(new LinearCompressibilityModel());
        var frozen = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal)
        {
            ["missing"] = FluidNodeBalance.Zero,
        };

        Assert.Throws<ArgumentException>(() => solver.StepSemiImplicit(state, TimeSpan.FromMilliseconds(10d), frozen));
    }

    private static PlantState CreateTwoNodeState(double massAKilograms, double massBKilograms)
    {
        var nodeA = new FluidNodeDefinition("a", Volume.FromCubicMetres(1d));
        var nodeB = new FluidNodeDefinition("b", Volume.FromCubicMetres(1d));
        var pipe = new PipeDefinition(
            "link",
            "a",
            "b",
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100d));
        var definition = new PlantDefinition(
            "stiff-pair",
            new[] { nodeA, nodeB },
            new[] { pipe },
            Array.Empty<ValveDefinition>(),
            Array.Empty<PumpDefinition>(),
            Array.Empty<ThermalBodyDefinition>(),
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());
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
            new[] { State(nodeA, massAKilograms), State(nodeB, massBKilograms) },
            Array.Empty<ValveState>(),
            Array.Empty<PumpState>(),
            Array.Empty<ThermalBodyState>(),
            Array.Empty<HeatSourceState>());
    }

    private static double PressureDifferenceMagnitude(PlantState state)
        => Math.Abs(state.GetFluidNode("a").Pressure.Pascals - state.GetFluidNode("b").Pressure.Pascals);

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
