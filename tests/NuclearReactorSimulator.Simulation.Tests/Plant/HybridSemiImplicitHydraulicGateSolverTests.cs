using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Plant;

public sealed class HybridSemiImplicitHydraulicGateSolverTests
{
    private static readonly TimeSpan StiffStep = TimeSpan.FromMilliseconds(100d);

    [Fact]
    public void Options_RejectInvalidTriggerValues()
    {
        var corrector = SemiImplicitHydraulicPrototypeOptions.H3AuditDefault;

        Assert.Throws<ArgumentOutOfRangeException>(() => new HybridSemiImplicitHydraulicGateOptions(-0.01d, 1d, corrector));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HybridSemiImplicitHydraulicGateOptions(0.01d, -1d, corrector));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HybridSemiImplicitHydraulicGateOptions(double.NaN, 1d, corrector));
        Assert.Throws<ArgumentNullException>(() => new HybridSemiImplicitHydraulicGateOptions(0.01d, 1d, null!));
    }

    [Fact]
    public void QuietGate_AcceptsExplicitPredictorWithoutCorrection()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var solver = new HybridSemiImplicitHydraulicGateSolver(new LinearCompressibilityModel());
        var options = new HybridSemiImplicitHydraulicGateOptions(
            predictedSubcooledPressureChangeTriggerFraction: 10d,
            predictedHydraulicFlowChangeTriggerKilogramsPerSecond: 1_000_000d,
            correctorOptions: SemiImplicitHydraulicPrototypeOptions.H3AuditDefault);

        var result = solver.Step(state, StiffStep, EmptyBalances(), options);

        Assert.False(result.UsedSemiImplicitCorrection);
        Assert.True(result.Converged);
        Assert.Equal(1, result.IterationCount);
    }

    [Fact]
    public void StiffGate_TriggersCorrectorAndSuppressesExplicitOvershoot()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var thermodynamics = new LinearCompressibilityModel();
        var prototype = new SemiImplicitHydraulicPrototypeSolver(thermodynamics);
        var hybrid = new HybridSemiImplicitHydraulicGateSolver(thermodynamics);
        var options = new HybridSemiImplicitHydraulicGateOptions(
            predictedSubcooledPressureChangeTriggerFraction: 0d,
            predictedHydraulicFlowChangeTriggerKilogramsPerSecond: 0d,
            correctorOptions: SemiImplicitHydraulicPrototypeOptions.H3AuditDefault);

        var explicitStep = prototype.StepExplicit(state, StiffStep, EmptyBalances());
        var result = hybrid.Step(state, StiffStep, EmptyBalances(), options);

        Assert.True(result.UsedSemiImplicitCorrection);
        Assert.True(result.Converged, result.ToString());
        Assert.True(result.IterationCount >= 2);
        Assert.True(PressureDifferenceMagnitude(result.CandidateState) < PressureDifferenceMagnitude(explicitStep.CandidateState));
    }

    [Fact]
    public void HistoricalExplicitPredictorReuse_IsExactlyEquivalentToLegacyPredictorEvaluation()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var thermodynamics = new LinearCompressibilityModel();
        var prototype = new SemiImplicitHydraulicPrototypeSolver(thermodynamics);
        var solver = new HybridSemiImplicitHydraulicGateSolver(thermodynamics);
        var explicitStep = prototype.StepExplicit(state, StiffStep, EmptyBalances());

        var legacy = solver.EvaluatePredictor(state, StiffStep, EmptyBalances());
        var reused = solver.EvaluatePredictorFromHistoricalExplicitCandidate(
            state,
            StiffStep,
            EmptyBalances(),
            explicitStep.HydraulicEvaluation,
            explicitStep.CandidateState,
            explicitStep.AppliedHydraulicBalances,
            out var reusedFluidNodeCount);

        Assert.Equal(state.FluidNodes.Count, reusedFluidNodeCount);

        Assert.Equal(legacy.UsedSemiImplicitCorrection, reused.UsedSemiImplicitCorrection);
        Assert.Equal(legacy.IterationCount, reused.IterationCount);
        Assert.Equal(legacy.Converged, reused.Converged);
        Assert.Equal(legacy.PredictorMaximumFractionalSubcooledPressureChange, reused.PredictorMaximumFractionalSubcooledPressureChange);
        Assert.Equal(legacy.PredictorMaximumAbsoluteHydraulicFlowChangeKilogramsPerSecond, reused.PredictorMaximumAbsoluteHydraulicFlowChangeKilogramsPerSecond);
        Assert.Equal(legacy.MaximumRelativePressureResidual, reused.MaximumRelativePressureResidual);
        Assert.Equal(legacy.MaximumAbsoluteFlowResidualKilogramsPerSecond, reused.MaximumAbsoluteFlowResidualKilogramsPerSecond);
        Assert.Equal(legacy.HydraulicEvaluation.MassRateClosureResidualKilogramsPerSecond, reused.HydraulicEvaluation.MassRateClosureResidualKilogramsPerSecond);
        Assert.Equal(legacy.HydraulicEvaluation.HydraulicEnergyOwnershipResidualWatts, reused.HydraulicEvaluation.HydraulicEnergyOwnershipResidualWatts);
        Assert.Equal(
            legacy.AppliedHydraulicBalances.OrderBy(static item => item.Key, StringComparer.Ordinal),
            reused.AppliedHydraulicBalances.OrderBy(static item => item.Key, StringComparer.Ordinal));
        Assert.Equal(
            legacy.HydraulicEvaluation.PipeMassFlowRates.OrderBy(static item => item.Key, StringComparer.Ordinal),
            reused.HydraulicEvaluation.PipeMassFlowRates.OrderBy(static item => item.Key, StringComparer.Ordinal));
        Assert.Equal(
            legacy.HydraulicEvaluation.ValveMassFlowRates.OrderBy(static item => item.Key, StringComparer.Ordinal),
            reused.HydraulicEvaluation.ValveMassFlowRates.OrderBy(static item => item.Key, StringComparer.Ordinal));
        Assert.Equal(
            legacy.HydraulicEvaluation.PumpMassFlowRates.OrderBy(static item => item.Key, StringComparer.Ordinal),
            reused.HydraulicEvaluation.PumpMassFlowRates.OrderBy(static item => item.Key, StringComparer.Ordinal));
        Assert.Equal(
            legacy.CandidateState.FluidNodes.Select(static node => (node.Id, node.Mass.Kilograms, node.InternalEnergy.Joules, node.Pressure.Pascals)),
            reused.CandidateState.FluidNodes.Select(static node => (node.Id, node.Mass.Kilograms, node.InternalEnergy.Joules, node.Pressure.Pascals)));
    }

    [Fact]
    public void HistoricalExplicitPredictorReuse_ReintegratesBalanceMismatchWithoutChangingPredictor()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var thermodynamics = new LinearCompressibilityModel();
        var prototype = new SemiImplicitHydraulicPrototypeSolver(thermodynamics);
        var solver = new HybridSemiImplicitHydraulicGateSolver(thermodynamics);
        var explicitStep = prototype.StepExplicit(state, StiffStep, EmptyBalances());
        var mismatchedHistoricalBalances = explicitStep.AppliedHydraulicBalances.ToDictionary(
            static item => item.Key,
            static item => item.Value,
            StringComparer.Ordinal);
        var firstNodeId = state.FluidNodes[0].Id;
        mismatchedHistoricalBalances[firstNodeId] = mismatchedHistoricalBalances[firstNodeId]
            + new FluidNodeBalance(MassFlowRate.FromKilogramsPerSecond(1e-9d), Power.Zero);

        var legacy = solver.EvaluatePredictor(state, StiffStep, EmptyBalances());
        var reused = solver.EvaluatePredictorFromHistoricalExplicitCandidate(
            state,
            StiffStep,
            EmptyBalances(),
            explicitStep.HydraulicEvaluation,
            explicitStep.CandidateState,
            mismatchedHistoricalBalances,
            out var reusedFluidNodeCount);

        Assert.Equal(state.FluidNodes.Count - 1, reusedFluidNodeCount);
        Assert.Equal(legacy.PredictorMaximumFractionalSubcooledPressureChange, reused.PredictorMaximumFractionalSubcooledPressureChange);
        Assert.Equal(legacy.PredictorMaximumAbsoluteHydraulicFlowChangeKilogramsPerSecond, reused.PredictorMaximumAbsoluteHydraulicFlowChangeKilogramsPerSecond);
        Assert.Equal(
            legacy.CandidateState.FluidNodes.Select(static node => (node.Id, node.Mass.Kilograms, node.InternalEnergy.Joules, node.Pressure.Pascals)),
            reused.CandidateState.FluidNodes.Select(static node => (node.Id, node.Mass.Kilograms, node.InternalEnergy.Joules, node.Pressure.Pascals)));
    }

    [Fact]
    public void HybridGate_IsExactlyDeterministicForSameInputs()
    {
        var state = CreateTwoNodeState(1_001d, 999d);
        var solver = new HybridSemiImplicitHydraulicGateSolver(new LinearCompressibilityModel());
        var options = new HybridSemiImplicitHydraulicGateOptions(
            predictedSubcooledPressureChangeTriggerFraction: 0.01d,
            predictedHydraulicFlowChangeTriggerKilogramsPerSecond: 1d,
            correctorOptions: new SemiImplicitHydraulicPrototypeOptions(64, 0.20d, 1e-5d, 1e-2d));

        var left = solver.Step(state, StiffStep, EmptyBalances(), options);
        var right = solver.Step(state, StiffStep, EmptyBalances(), options);

        Assert.Equal(left.UsedSemiImplicitCorrection, right.UsedSemiImplicitCorrection);
        Assert.Equal(left.IterationCount, right.IterationCount);
        Assert.Equal(left.Converged, right.Converged);
        Assert.Equal(left.PredictorMaximumFractionalSubcooledPressureChange, right.PredictorMaximumFractionalSubcooledPressureChange);
        Assert.Equal(left.PredictorMaximumAbsoluteHydraulicFlowChangeKilogramsPerSecond, right.PredictorMaximumAbsoluteHydraulicFlowChangeKilogramsPerSecond);
        Assert.Equal(left.MaximumRelativePressureResidual, right.MaximumRelativePressureResidual);
        Assert.Equal(left.MaximumAbsoluteFlowResidualKilogramsPerSecond, right.MaximumAbsoluteFlowResidualKilogramsPerSecond);
        Assert.Equal(
            left.CandidateState.FluidNodes.Select(static node => (node.Id, node.Mass.Kilograms, node.InternalEnergy.Joules, node.Pressure.Pascals)),
            right.CandidateState.FluidNodes.Select(static node => (node.Id, node.Mass.Kilograms, node.InternalEnergy.Joules, node.Pressure.Pascals)));
    }

    private static Dictionary<string, FluidNodeBalance> EmptyBalances()
        => new(StringComparer.Ordinal);

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
            "hybrid-gate-stiff-pair",
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
