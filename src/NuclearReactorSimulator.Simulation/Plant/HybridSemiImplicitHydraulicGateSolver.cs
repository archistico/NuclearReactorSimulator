using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Deterministic hybrid gate introduced by Phase H.4 and selected for opt-in current-v2 production in H.5.
/// Every logical step first computes the existing explicit predictor. A semi-implicit H.3 correction is
/// evaluated only when the predictor crosses a deterministic pressure/flow stiffness threshold.
/// </summary>
public sealed class HybridSemiImplicitHydraulicGateSolver
{
    private readonly SemiImplicitHydraulicPrototypeSolver _prototypeSolver;

    public HybridSemiImplicitHydraulicGateSolver(IFluidThermodynamicModel thermodynamicModel)
    {
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        _prototypeSolver = new SemiImplicitHydraulicPrototypeSolver(thermodynamicModel);
    }

    public HybridSemiImplicitHydraulicGateStepResult Step(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        HybridSemiImplicitHydraulicGateOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var predictor = _prototypeSolver.StepExplicit(committedState, deltaTime, frozenNonHydraulicBalances);
        var predictorEndEvaluation = _prototypeSolver.Evaluate(predictor.CandidateState);
        var predictedPressureChange = MaximumFractionalSubcooledPressureChange(
            committedState,
            predictor.CandidateState);
        var predictedFlowChange = MaximumAbsoluteFlowDifference(
            predictor.HydraulicEvaluation,
            predictorEndEvaluation);

        if (!options.RequiresCorrection(predictedPressureChange, predictedFlowChange))
        {
            return new HybridSemiImplicitHydraulicGateStepResult(
                predictor.CandidateState,
                predictorEndEvaluation,
                predictor.AppliedHydraulicBalances,
                false,
                predictor.IterationCount,
                predictor.Converged,
                predictedPressureChange,
                predictedFlowChange,
                predictor.MaximumRelativePressureResidual,
                predictor.MaximumAbsoluteFlowResidualKilogramsPerSecond);
        }

        var corrected = _prototypeSolver.StepSemiImplicit(
            committedState,
            deltaTime,
            frozenNonHydraulicBalances,
            options.CorrectorOptions);

        return new HybridSemiImplicitHydraulicGateStepResult(
            corrected.CandidateState,
            corrected.HydraulicEvaluation,
            corrected.AppliedHydraulicBalances,
            true,
            corrected.IterationCount,
            corrected.Converged,
            predictedPressureChange,
            predictedFlowChange,
            corrected.MaximumRelativePressureResidual,
            corrected.MaximumAbsoluteFlowResidualKilogramsPerSecond);
    }

    private static double MaximumFractionalSubcooledPressureChange(PlantState start, PlantState end)
    {
        var endNodes = end.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var maximum = 0d;
        foreach (var startNode in start.FluidNodes.Where(static node => node.Phase == FluidPhase.SubcooledLiquid))
        {
            var endPressure = endNodes[startNode.Id].Pressure.Pascals;
            var scale = Math.Max(Math.Abs(startNode.Pressure.Pascals), 1_000d);
            maximum = Math.Max(maximum, Math.Abs(endPressure - startNode.Pressure.Pascals) / scale);
        }

        return maximum;
    }

    private static double MaximumAbsoluteFlowDifference(
        SemiImplicitHydraulicEvaluation start,
        SemiImplicitHydraulicEvaluation end)
    {
        var maximum = 0d;
        maximum = Math.Max(maximum, MaximumDifference(start.PipeMassFlowRates, end.PipeMassFlowRates));
        maximum = Math.Max(maximum, MaximumDifference(start.ValveMassFlowRates, end.ValveMassFlowRates));
        maximum = Math.Max(maximum, MaximumDifference(start.PumpMassFlowRates, end.PumpMassFlowRates));
        return maximum;
    }

    private static double MaximumDifference(
        IReadOnlyDictionary<string, MassFlowRate> start,
        IReadOnlyDictionary<string, MassFlowRate> end)
    {
        var maximum = 0d;
        foreach (var entry in start)
        {
            maximum = Math.Max(
                maximum,
                Math.Abs(end[entry.Key].KilogramsPerSecond - entry.Value.KilogramsPerSecond));
        }

        return maximum;
    }
}
